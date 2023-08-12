using Katuusagi.ConstExpressionForUnity.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Katuusagi.ConstExpressionForUnity.Editor
{
    internal class ConstExpressionILPostProcessor : ILPostProcessor
    {
        private static readonly Regex StackTraceCheck = new Regex("at .*\\(.*\\) in .*\\:line [0-9]{1,}");
        private static readonly Regex StackTracePrefix = new Regex("at .*\\(.*\\) in ");
        private static readonly Regex StackTraceSuffix = new Regex(" in .*\\:line [0-9]{1,}");

        private static Dictionary<string, Delegate> _constExprTable = null;

        private List<DiagnosticMessage> _messages = new List<DiagnosticMessage>();
        private Dictionary<object, FieldReference> _constFields = new Dictionary<object, FieldReference>(new FieldsComparer());
        private TypeDefinition _constTableType;
        private MethodDefinition _constTableConstructor;

        private class FieldsComparer : IEqualityComparer<object>
        {
            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                if (x is IEnumerable xe &&
                    y is IEnumerable ye)
                {
                    return xe.Cast<object>().SequenceEqual(ye.Cast<object>());
                }

                return x.Equals(y);
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                if (!(obj is IEnumerable oe))
                {
                    return obj.GetHashCode();
                }

                const int prime = 31;
                int hash = 1;

                foreach (object o in oe)
                {
                    hash = hash * prime + o.GetHashCode();
                }

                return hash;
            }
        }

        public override ILPostProcessor GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return true;
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
            {
                return null;
            }

            _messages.Clear();

            try
            {
                _constExprTable = FindConstExprMethods();
                using (var assembly = LoadAssemblyDefinition(compiledAssembly))
                {
                    var module = assembly.MainModule;
                    _constTableType = new TypeDefinition("Katuusagi.ConstExpressionForUnity.Generated", "$ConstTable", Mono.Cecil.TypeAttributes.Class | Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.Abstract);
                    _constTableType.BaseType = module.TypeSystem.Object;

                    var cctorAttr = Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName;
                    _constTableConstructor = new MethodDefinition(".cctor", cctorAttr, module.TypeSystem.Void);
                    _constTableType.Methods.Add(_constTableConstructor);
                    module.Types.Add(_constTableType);

                    foreach (var type in assembly.Modules.SelectMany(v => GetTypes(v.Types)))
                    {
                        if (!type.HasMethods)
                        {
                            continue;
                        }

                        var clonedMethods = type.Methods.ToArray();
                        int sizeDiff = 0;
                        foreach (var method in clonedMethods)
                        {
                            var body = method.Body;
                            if (body == null)
                            {
                                continue;
                            }

                            var ilProcessor = body.GetILProcessor();
                            var instructions = body.Instructions;
                            for (var i = 0; i < instructions.Count; ++i)
                            {
                                var instruction = instructions[i];
                                ConstExpressionProcess(ilProcessor, method, instruction, out var diff, ref sizeDiff);
                                OpLabelProcess(instruction, sizeDiff);
                                i += diff;
                            }
                        }
                    }

                    _constTableConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                    var pe  = new MemoryStream();
                    var pdb = new MemoryStream();
                    var writeParameter = new WriterParameters
                    {
                        SymbolWriterProvider = new PortablePdbWriterProvider(),
                        SymbolStream         = pdb,
                        WriteSymbols         = true
                    };

                    assembly.Write(pe, writeParameter);
                    return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), _messages);
                }
            }
            catch (Exception e)
            {
                LogError($"{e.GetType()}:{e.Message}", e.StackTrace);
            }
            return new ILPostProcessResult(null, _messages);
        }

        private void OpLabelProcess(Instruction instruction, int sizeDiff)
        {
            if (sizeDiff == 0)
            {
                return;
            }

            {
                if (instruction.Operand is Instruction target)
                {
                    target.Offset += sizeDiff;
                    return;
                }
            }

            if (instruction.Operand is Instruction[] targets)
            {
                foreach (var target in targets)
                {
                    target.Offset += sizeDiff;
                }

                return;
            }
        }

        private void ConstExpressionProcess(ILProcessor ilProcessor, MethodDefinition method, Instruction instruction, out int instructionDiff, ref int sizeDiff)
        {
            instructionDiff = 0;
            if (instruction.OpCode != OpCodes.Call ||
                !_constExprTable.TryGetValue(instruction.Operand.ToString(), out var constExpr))
            {
                return;
            }

            var parameters = constExpr.Method.GetParameters();
            var args = new object[parameters.Length];
            var argInstruction = instruction;
            if (parameters.Any())
            {
                for (int j = parameters.Length - 1; j >= 0; --j)
                {
                    argInstruction = argInstruction.Previous;
                    if (!TryEmulateLiteral(ref argInstruction, parameters[j].ParameterType, out var arg))
                    {
                        if (constExpr.Method.GetCustomAttribute<ConstExpressionAttribute>().CalculationFailedWarning)
                        {
                            LogWarning($"ConstExpression calculation failed.", method, argInstruction);
                        }
                        return;
                    }

                    args[j] = arg;
                }
            }

            object literal;
            try
            {
                literal = constExpr.DynamicInvoke(args);
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException t &&
                    t.InnerException != null)
                {
                    e = t.InnerException;
                }

                LogError($"{e.GetType()}:{e.Message}", e.StackTrace);
                return;
            }

            while (argInstruction != instruction)
            {
                argInstruction = argInstruction.Next;
                sizeDiff -= argInstruction.Previous.GetSize();
                --instructionDiff;
                ilProcessor.Remove(argInstruction.Previous);
            }

            var loadLiteral = LoadLiteral(ilProcessor, literal);
            sizeDiff += loadLiteral.GetSize();
            ++instructionDiff;
            ilProcessor.InsertBefore(instruction, loadLiteral);

            sizeDiff -= instruction.GetSize();
            --instructionDiff;
            ilProcessor.Remove(instruction);
        }

        private IEnumerable<TypeDefinition> GetTypes(IEnumerable<TypeDefinition> types)
        {
            foreach (var type in types)
            {
                yield return type;
                foreach (var nested in GetTypes(type.NestedTypes))
                {
                    yield return nested;
                }
            }
        }

        private Instruction SetElement(object literal)
        {
            var literalType = literal.GetType();
            if (literal is Enum enumValue)
            {
                var underlyingType = Enum.GetUnderlyingType(literalType);
                if (underlyingType == typeof(sbyte))
                {
                    literal = (sbyte)(object)enumValue;
                }
                else if (underlyingType == typeof(byte))
                {
                    literal = (byte)(object)enumValue;
                }
                else if (underlyingType == typeof(short))
                {
                    literal = (short)(object)enumValue;
                }
                else if (underlyingType == typeof(ushort))
                {
                    literal = (ushort)(object)enumValue;
                }
                else if (underlyingType == typeof(int))
                {
                    literal = (int)(object)enumValue;
                }
                else if (underlyingType == typeof(uint))
                {
                    literal = (uint)(object)enumValue;
                }
                else if (underlyingType == typeof(long))
                {
                    literal = (long)(object)enumValue;
                }
                else if (underlyingType == typeof(ulong))
                {
                    literal = (ulong)(object)enumValue;
                }
            }

            if (literal is sbyte)
            {
                return Instruction.Create(OpCodes.Stelem_I1);
            }
            if (literal is byte)
            {
                return Instruction.Create(OpCodes.Stelem_I1);
            }
            if (literal is short)
            {
                return Instruction.Create(OpCodes.Stelem_I2);
            }
            if (literal is ushort)
            {
                return Instruction.Create(OpCodes.Stelem_I2);
            }
            if (literal is int)
            {
                return Instruction.Create(OpCodes.Stelem_I4);
            }
            if (literal is uint)
            {
                return Instruction.Create(OpCodes.Stelem_I4);
            }
            if (literal is long)
            {
                return Instruction.Create(OpCodes.Stelem_I8);
            }
            if (literal is ulong)
            {
                return Instruction.Create(OpCodes.Stelem_I8);
            }
            if (literal is float)
            {
                return Instruction.Create(OpCodes.Stelem_R4);
            }
            if (literal is double)
            {
                return Instruction.Create(OpCodes.Stelem_R8);
            }
            if (literal is char)
            {
                return Instruction.Create(OpCodes.Stelem_I2);
            }
            if (literal is string)
            {
                return Instruction.Create(OpCodes.Stelem_Ref);
            }

            return null;
        }

        private Instruction LoadLiteral(ILProcessor ilProcessor, object literal)
        {
            var literalType = literal.GetType();
            if (literal is Enum enumValue)
            {
                var underlyingType = Enum.GetUnderlyingType(literalType);
                if (underlyingType == typeof(sbyte))
                {
                    literal = (sbyte)(object)enumValue;
                }
                else if (underlyingType == typeof(byte))
                {
                    literal = (byte)(object)enumValue;
                }
                else if (underlyingType == typeof(short))
                {
                    literal = (short)(object)enumValue;
                }
                else if (underlyingType == typeof(ushort))
                {
                    literal = (ushort)(object)enumValue;
                }
                else if (underlyingType == typeof(int))
                {
                    literal = (int)(object)enumValue;
                }
                else if (underlyingType == typeof(uint))
                {
                    literal = (uint)(object)enumValue;
                }
                else if (underlyingType == typeof(long))
                {
                    literal = (long)(object)enumValue;
                }
                else if (underlyingType == typeof(ulong))
                {
                    literal = (ulong)(object)enumValue;
                }
            }
            
            if (literal is sbyte sbyteValue)
            {
                literal = (int)sbyteValue;
            }
            else if (literal is byte byteValue)
            {
                literal = (int)byteValue;
            }
            else if (literal is short shortValue)
            {
                literal = (int)shortValue;
            }
            else if (literal is ushort ushortValue)
            {
                literal = (int)ushortValue;
            }
            else if (literal is uint uintValue)
            {
                literal = (int)uintValue;
            }
            else if (literal is char charValue)
            {
                literal = (int)charValue;
            }

            if (literal is int intValue)
            {
                switch (intValue)
                {
                    case -1:
                        return Instruction.Create(OpCodes.Ldc_I4_M1);
                    case 0:
                        return Instruction.Create(OpCodes.Ldc_I4_0);
                    case 1:
                        return Instruction.Create(OpCodes.Ldc_I4_1);
                    case 2:
                        return Instruction.Create(OpCodes.Ldc_I4_2);
                    case 3:
                        return Instruction.Create(OpCodes.Ldc_I4_3);
                    case 4:
                        return Instruction.Create(OpCodes.Ldc_I4_4);
                    case 5:
                        return Instruction.Create(OpCodes.Ldc_I4_5);
                    case 6:
                        return Instruction.Create(OpCodes.Ldc_I4_6);
                    case 7:
                        return Instruction.Create(OpCodes.Ldc_I4_7);
                    case 8:
                        return Instruction.Create(OpCodes.Ldc_I4_8);
                }

                /*
                if (-128 <= intValue && intValue < 128)
                {
                    return Instruction.Create(OpCodes.Ldc_I4_S, intValue);
                }
                */

                return Instruction.Create(OpCodes.Ldc_I4, intValue);
            }

            if (literal is ulong ulongValue)
            {
                literal = (long)ulongValue;
            }

            if (literal is long longValue)
            {
                return Instruction.Create(OpCodes.Ldc_I8, longValue);
            }

            if (literal is float floatValue)
            {
                return Instruction.Create(OpCodes.Ldc_R4, floatValue);
            }

            if (literal is double doubleValue)
            {
                return Instruction.Create(OpCodes.Ldc_R8, doubleValue);
            }

            if (literal is string stringValue)
            {
                return Instruction.Create(OpCodes.Ldstr, stringValue);
            }

            if (literal is IEnumerable array)
            {
                var field = GetReadOnlyArrayField(ilProcessor, array);
                return Instruction.Create(OpCodes.Ldsfld, field);
            }

            if (!literalType.IsClass && !literalType.IsInterface)
            {
                var field = GetStructField(ilProcessor, literal);
                return Instruction.Create(OpCodes.Ldsfld, field);
            }

            return null;
        }

        private bool TryEmulateLiteral(ref Instruction instruction, Type type, out object result)
        {
            if (!TryEmulateLiteral(ref instruction, out result))
            {
                return false;
            }

            if (result is string)
            {
                if (type == typeof(string))
                {
                    return true;
                }

                return false;
            }

            if (result is int intValue)
            {
                if (type == typeof(sbyte))
                {
                    result = (sbyte)intValue;
                    return true;
                }

                if (type == typeof(byte))
                {
                    result = (byte)intValue;
                    return true;
                }

                if (type == typeof(short))
                {
                    result = (short)intValue;
                    return true;
                }

                if (type == typeof(ushort))
                {
                    result = (ushort)intValue;
                    return true;
                }

                if (type == typeof(int))
                {
                    return true;
                }

                if (type == typeof(uint))
                {
                    result = (uint)intValue;
                    return true;
                }

                if (type == typeof(long))
                {
                    result = (long)intValue;
                    return true;
                }

                if (type == typeof(ulong))
                {
                    result = (ulong)intValue;
                    return true;
                }

                if (type == typeof(float))
                {
                    result = (float)intValue;
                    return true;
                }

                if (type == typeof(double))
                {
                    result = (double)intValue;
                    return true;
                }

                if (type == typeof(char))
                {
                    result = (char)intValue;
                    return true;
                }

                if (type.IsEnum)
                {
                    result = intValue;
                    return true;
                }

                return false;
            }

            if (result is long longValue)
            {
                if (type == typeof(sbyte))
                {
                    result = (sbyte)longValue;
                    return true;
                }

                if (type == typeof(byte))
                {
                    result = (byte)longValue;
                    return true;
                }

                if (type == typeof(short))
                {
                    result = (short)longValue;
                    return true;
                }

                if (type == typeof(ushort))
                {
                    result = (ushort)longValue;
                    return true;
                }

                if (type == typeof(int))
                {
                    result = (int)longValue;
                    return true;
                }

                if (type == typeof(uint))
                {
                    result = (uint)longValue;
                    return true;
                }

                if (type == typeof(long))
                {
                    return true;
                }

                if (type == typeof(ulong))
                {
                    result = (ulong)longValue;
                    return true;
                }

                if (type == typeof(float))
                {
                    result = (float)longValue;
                    return true;
                }

                if (type == typeof(double))
                {
                    result = (double)longValue;
                    return true;
                }

                if (type == typeof(char))
                {
                    result = (char)longValue;
                    return true;
                }

                if (type.IsEnum)
                {
                    result = longValue ;
                    return true;
                }

                return false;
            }

            if (result is float floatValue)
            {
                if (type == typeof(sbyte))
                {
                    result = (sbyte)floatValue;
                    return true;
                }

                if (type == typeof(byte))
                {
                    result = (byte)floatValue;
                    return true;
                }

                if (type == typeof(short))
                {
                    result = (short)floatValue;
                    return true;
                }

                if (type == typeof(ushort))
                {
                    result = (ushort)floatValue;
                    return true;
                }

                if (type == typeof(int))
                {
                    result = (int)floatValue;
                    return true;
                }

                if (type == typeof(uint))
                {
                    result = (uint)floatValue;
                    return true;
                }

                if (type == typeof(long))
                {
                    result = (long)floatValue;
                    return true;
                }

                if (type == typeof(ulong))
                {
                    result = (ulong)floatValue;
                    return true;
                }

                if (type == typeof(float))
                {
                    return true;
                }

                if (type == typeof(double))
                {
                    result = (double)floatValue;
                    return true;
                }

                if (type == typeof(char))
                {
                    result = (char)floatValue;
                    return true;
                }

                return false;
            }

            if (result is double doubleValue)
            {
                if (type == typeof(sbyte))
                {
                    result = (sbyte)doubleValue;
                    return true;
                }

                if (type == typeof(byte))
                {
                    result = (byte)doubleValue;
                    return true;
                }

                if (type == typeof(short))
                {
                    result = (short)doubleValue;
                    return true;
                }

                if (type == typeof(ushort))
                {
                    result = (ushort)doubleValue;
                    return true;
                }

                if (type == typeof(int))
                {
                    result = (int)doubleValue;
                    return true;
                }

                if (type == typeof(uint))
                {
                    result = (uint)doubleValue;
                    return true;
                }

                if (type == typeof(long))
                {
                    result = (long)doubleValue;
                    return true;
                }

                if (type == typeof(ulong))
                {
                    result = (ulong)doubleValue;
                    return true;
                }

                if (type == typeof(float))
                {
                    result = (float)doubleValue;
                    return true;
                }

                if (type == typeof(double))
                {
                    return true;
                }

                if (type == typeof(char))
                {
                    result = (char)doubleValue;
                    return true;
                }

                return false;
            }

            return false;
        }

        private bool TryEmulateLiteral(ref Instruction instruction, out object result)
        {
            if (instruction.OpCode == OpCodes.Ldstr)
            {
                if (instruction.Operand is string)
                {
                    result = instruction.Operand;
                    return true;
                }

                result = instruction.Operand.ToString();
                return true;
            }
            if (instruction.OpCode == OpCodes.Ldc_I4_0)
            {
                result = 0;
                return true;
            }
            if (instruction.OpCode == OpCodes.Ldc_I4_1)
            {
                result = 1;
                return true;
            }
            if (instruction.OpCode == OpCodes.Ldc_I4_2)
            {
                result = 2;
                return true;
            }
            if (instruction.OpCode == OpCodes.Ldc_I4_3)
            {
                result = 3;
                return true;
            }
            if (instruction.OpCode == OpCodes.Ldc_I4_4)
            {
                result = 4;
                return true;
            }
            if (instruction.OpCode == OpCodes.Ldc_I4_5)
            {
                result = 5;
                return true;
            }
            if (instruction.OpCode == OpCodes.Ldc_I4_6)
            {
                result = 6;
                return true;
            }
            if (instruction.OpCode == OpCodes.Ldc_I4_7)
            {
                result = 7;
                return true;
            }
            if (instruction.OpCode == OpCodes.Ldc_I4_8)
            {
                result = 8;
                return true;
            }
            if (instruction.OpCode == OpCodes.Ldc_I4_M1)
            {
                result = -1;
                return true;
            }
            if (instruction.OpCode == OpCodes.Ldc_I4_S || instruction.OpCode == OpCodes.Ldc_I4)
            {
                if (instruction.Operand is int)
                {
                    result = instruction.Operand;
                    return true;
                }

                result = int.Parse(instruction.Operand.ToString());
                return true;
            }

            if (instruction.OpCode == OpCodes.Ldc_I8)
            {
                if (instruction.Operand is long)
                {
                    result = instruction.Operand;
                    return true;
                }

                result = long.Parse(instruction.Operand.ToString());
                return true;
            }

            if (instruction.OpCode == OpCodes.Ldc_R4)
            {
                if (instruction.Operand is float)
                {
                    result = instruction.Operand;
                    return true;
                }

                result = float.Parse(instruction.Operand.ToString());
                return true;
            }

            if (instruction.OpCode == OpCodes.Ldc_R8)
            {
                if (instruction.Operand is double)
                {
                    result = instruction.Operand;
                    return true;
                }

                result = double.Parse(instruction.Operand.ToString());
                return true;
            }

            if (instruction.OpCode == OpCodes.Conv_I1)
            {
                instruction = instruction.Previous;
                if (!TryEmulateLiteral(ref instruction, out result))
                {
                    return false;
                }

                if (result is int intValue)
                {
                    result = (sbyte)intValue;
                    return true;
                }
                if (result is long longValue)
                {
                    result = (sbyte)longValue;
                    return true;
                }
                if (result is float floatValue)
                {
                    result = (sbyte)floatValue;
                    return true;
                }
                if (result is double doubleValue)
                {
                    result = (sbyte)doubleValue;
                    return true;
                }
            }

            if (instruction.OpCode == OpCodes.Conv_I2)
            {
                instruction = instruction.Previous;
                if (!TryEmulateLiteral(ref instruction, out result))
                {
                    return false;
                }

                if (result is int intValue)
                {
                    result = (short)intValue;
                    return true;
                }
                if (result is long longValue)
                {
                    result = (short)longValue;
                    return true;
                }
                if (result is float floatValue)
                {
                    result = (short)floatValue;
                    return true;
                }
                if (result is double doubleValue)
                {
                    result = (short)doubleValue;
                    return true;
                }
            }

            if (instruction.OpCode == OpCodes.Conv_I4)
            {
                instruction = instruction.Previous;
                if (!TryEmulateLiteral(ref instruction, out result))
                {
                    return false;
                }

                if (result is int intValue)
                {
                    result = intValue;
                    return true;
                }
                if (result is long longValue)
                {
                    result = (int)longValue;
                    return true;
                }
                if (result is float floatValue)
                {
                    result = (int)floatValue;
                    return true;
                }
                if (result is double doubleValue)
                {
                    result = (int)doubleValue;
                    return true;
                }
            }

            if (instruction.OpCode == OpCodes.Conv_I8)
            {
                instruction = instruction.Previous;
                if (!TryEmulateLiteral(ref instruction, out result))
                {
                    return false;
                }

                if (result is int intValue)
                {
                    result = (long)intValue;
                    return true;
                }
                if (result is long longValue)
                {
                    result = longValue;
                    return true;
                }
                if (result is float floatValue)
                {
                    result = (long)floatValue;
                    return true;
                }
                if (result is double doubleValue)
                {
                    result = (long)doubleValue;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private FieldReference GetStructField(ILProcessor ilProcessor, object obj)
        {
            if (_constFields.TryGetValue(obj, out FieldReference value))
            {
                return value;
            }

            var type = _constTableType;
            var cctor = _constTableConstructor;

            // インポート
            var objType = type.Module.ImportReference(obj.GetType());

            // 初期値相当のメンバ変数を作成
            var field = new FieldDefinition($"${_constFields.Count}", Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.InitOnly, objType);
            type.Fields.Add(field);

            int size = Marshal.SizeOf(obj);
            byte[] array = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, false);
            Marshal.Copy(ptr, array, 0, size);
            Marshal.FreeHGlobal(ptr);

            // 静的コンストラクタに初期化処理を書く
            var instructions = cctor.Body.Instructions;
            instructions.Add(LoadLiteral(ilProcessor, array.Length));
            instructions.Add(Instruction.Create(OpCodes.Conv_U));
            instructions.Add(Instruction.Create(OpCodes.Localloc));
            instructions.Add(Instruction.Create(OpCodes.Dup));
            instructions.Add(LoadLiteral(ilProcessor, array[0]));
            instructions.Add(Instruction.Create(OpCodes.Stind_I1));

            for (int i = 1; i < array.Length; ++i)
            {
                var e = array[i];
                instructions.Add(Instruction.Create(OpCodes.Dup));
                instructions.Add(LoadLiteral(ilProcessor, i));
                instructions.Add(Instruction.Create(OpCodes.Add));
                instructions.Add(LoadLiteral(ilProcessor, e));
                instructions.Add(Instruction.Create(OpCodes.Stind_I1));
            }
            instructions.Add(Instruction.Create(OpCodes.Ldobj, objType));
            instructions.Add(Instruction.Create(OpCodes.Stsfld, field));

            // メンバ変数情報をテーブルに保持
            value = field;
            _constFields.Add(obj, value);
            return value;
        }

        private FieldReference GetReadOnlyArrayField(ILProcessor ilProcessor, IEnumerable array)
        {
            var arrayType = array.GetType();
            if (!arrayType.IsGenericType ||
                arrayType.GetGenericTypeDefinition() != typeof(ReadOnlyArray<>))
            {
                return null;
            }

            if (_constFields.TryGetValue(array, out FieldReference value))
            {
                return value;
            }

            var elementType = arrayType.GetGenericArguments()[0];

            var type = _constTableType;
            var cctor = _constTableConstructor;

            // インポート
            var elementTypeRef = type.Module.ImportReference(elementType);
            var arrayTypeRef = type.Module.ImportReference(arrayType);
            var implicitMethod = typeof(ReadOnlyArrayUtils).GetMethod("ConvertArrayToReadOnlyArray", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(elementType);
            var arrayTypeCast = type.Module.ImportReference(implicitMethod);

            // 初期値相当のメンバ変数を作成
            var field = new FieldDefinition($"${_constFields.Count}", Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.InitOnly, arrayTypeRef);
            type.Fields.Add(field);

            // 静的コンストラクタに初期化処理を書く
            var instructions = cctor.Body.Instructions;
            var count = Count(array);
            instructions.Add(LoadLiteral(ilProcessor, count));
            instructions.Add(Instruction.Create(OpCodes.Newarr, elementTypeRef));

            int i = 0;
            foreach (var e in array)
            {
                instructions.Add(Instruction.Create(OpCodes.Dup));
                instructions.Add(LoadLiteral(ilProcessor, i));
                instructions.Add(LoadLiteral(ilProcessor, e));
                instructions.Add(SetElement(e));
                ++i;
            }
            instructions.Add(Instruction.Create(OpCodes.Call, arrayTypeCast));
            instructions.Add(Instruction.Create(OpCodes.Stsfld, field));

            // メンバ変数情報をテーブルに保持
            value = field;
            _constFields.Add(array, value);
            return value;
        }

        private int Count(IEnumerable enumerable)
        {
            int c = 0;
            foreach (var e in enumerable)
            {
                ++c;
            }
            return c;
        }

        private IEnumerable<MethodInfo> FindMethods<T>()
            where T : Attribute
        {
            var assembly = typeof(ConstExpressionEntry).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods())
                {
                    if (method.GetCustomAttribute<T>() == null)
                    {
                        continue;
                    }

                    yield return method;
                }
            }
        }

        private Dictionary<string, Delegate> FindConstExprMethods()
        {
            var result = new Dictionary<string, Delegate>();
            foreach (var method in FindMethods<ConstExpressionAttribute>())
            {
                if (!CheckMethodError(method))
                {
                    continue;
                }

                var delType = GetDelegateType(method.GetParameters().Select(v => v.ParameterType).Append(method.ReturnType).ToArray());
                var del = Delegate.CreateDelegate(delType, method);
                var args = string.Empty;
                foreach (var parameter in method.GetParameters())
                {
                    args = $"{args}{GetTypeName(parameter.ParameterType)},";
                }

                if (!string.IsNullOrEmpty(args))
                {
                    args = args.Remove(args.Length - 1, 1);
                }
                string returnName = GetTypeName(method.ReturnType);
                result.Add($"{returnName} {method.ReflectedType.FullName}::{method.Name}({args})", del);
            }

            return result;
        }

        private string GetTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                string generic = string.Empty;
                foreach (var arg in type.GetGenericArguments())
                {
                    generic += $"{GetTypeName(arg)},";
                }
                generic = generic.Remove(generic.Length - 1, 1);

                string parentName;
                if (type.ReflectedType != null)
                {
                    parentName = $"{GetTypeName(type)}/";
                }
                else
                {
                    parentName = $"{type.Namespace}.";
                }

                return $"{parentName}{type.Name}<{generic}>";
            }

            return type.FullName.Replace("+", "/");
        }

        private string GetMethodName(MethodInfo method)
        {
            string parameters = string.Empty;
            if (method.GetParameters().Any())
            {
                foreach (var arg in method.GetParameters())
                {
                    parameters += $"{arg.ParameterType.Name},";
                }
            }
            parameters = parameters.Remove(parameters.Length - 1, 1);
            return $"{method.ReflectedType.FullName}.{method.Name}({parameters})";
        }

        private string GetMethodName(MethodDefinition method)
        {
            string parameters = string.Empty;
            if (method.Parameters.Any())
            {
                foreach (var arg in method.Parameters)
                {
                    parameters += $"{arg.ParameterType.Name},";
                }
                parameters = parameters.Remove(parameters.Length - 1, 1);
            }
            return $"{method.DeclaringType.FullName}.{method.Name}({parameters})";
        }

        private bool CheckMethodError(MethodInfo method)
        {
            if (!method.IsStatic)
            {
                LogError($"ConstExpression is static method only.", method);
                return false;
            }

            if (!IsAllowConstExpressionReturnType(method.ReturnType))
            {
                LogError($"ConstExpression does not support return of type \"{method.ReturnType.FullName}\".", method);
                return false;
            }

            foreach (var parameter in method.GetParameters())
            {
                if (IsAllowConstExpressionParameterType(parameter.ParameterType))
                {
                    continue;
                }

                LogError($"ConstExpression does not support parameters of type \"{method.ReturnType.FullName}\".", method);
                return false;
            }

            return true;
        }

        private static bool IsAllowConstExpressionParameterType(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type.IsEnum;
        }

        private static bool IsAllowConstExpressionReturnType(Type type)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(ReadOnlyArray<>))
                {
                    var etype = type.GetGenericArguments()[0];
                    return etype.IsPrimitive || etype == typeof(string) || etype.IsEnum;
                }
            }

            if (type == typeof(string) ||
                type.IsEnum)
            {
                return true;
            }

            if (!IsStructRecursive(type))
            {
                return false;
            }

            try
            {
                Marshal.SizeOf(type);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsStructRecursive(Type type)
        {
            if (type.IsPrimitive || type.IsEnum)
            {
                return true;
            }

            if (type.IsClass || type.IsInterface)
            {
                return false;
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return fields.Select(v => v.FieldType).All(IsStructRecursive);
        }

        private static Type GetDelegateType(Type[] args)
        {
            var count = args.Length;
            var typeName = $"System.Func`{count}";
            var funcType = Type.GetType(typeName);

            return funcType.MakeGenericType(args);
        }

        private static AssemblyDefinition LoadAssemblyDefinition(ICompiledAssembly compiledAssembly)
        {
            var resolver = new PostProcessorAssemblyResolver(compiledAssembly);
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData.ToArray()),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = resolver,
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider(),
                ReadingMode = ReadingMode.Immediate,
                ReadSymbols = true,
            };

            var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData.ToArray());
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(peStream, readerParameters);

            resolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);

            return assemblyDefinition;
        }

        private void LogWarning(object o)
        {
            _messages.Add(new DiagnosticMessage()
            {
                DiagnosticType = DiagnosticType.Warning,
                MessageData = o.ToString(),
            });
        }

        private void LogWarning(object o, MethodDefinition method, Instruction instruction)
        {
            var point = method.DebugInformation.GetSequencePoint(instruction);
            if (point == null)
            {
                LogWarning($"{o}  at {GetMethodName(method)}");
                return;
            }

            var file = point.Document.Url.Replace("\\", "/");
            file = file.Remove(0, file.IndexOf("/Assets/") + 1);
            LogWarning($"{o}", file, point.StartLine, point.StartColumn);
        }

        private void LogWarning(object o, string file, int line, int column)
        {
            _messages.Add(new DiagnosticMessage()
            {
                DiagnosticType = DiagnosticType.Warning,
                MessageData = o.ToString(),
                File = file,
                Line = line,
                Column = column,
            });
        }

        private void LogError(object o)
        {
            _messages.Add(new DiagnosticMessage()
            {
                DiagnosticType = DiagnosticType.Error,
                MessageData = o.ToString(),
            });
        }

        private void LogError(object o, MethodInfo method)
        {
            LogError($"{o}  at {GetMethodName(method)}");
        }

        private void LogError(object o, string stacktrace)
        {
            var splitedStackTraces = stacktrace.Split('\n');
            stacktrace = splitedStackTraces.FirstOrDefault(v => StackTraceCheck.IsMatch(v));
            if (stacktrace == null)
            {
                stacktrace = splitedStackTraces.FirstOrDefault();
                LogError($"{o}{stacktrace}");
                return;
            }

            var method = StackTraceSuffix.Replace(stacktrace, string.Empty);
            stacktrace = StackTracePrefix.Replace(stacktrace, string.Empty);
            var traceElements = stacktrace.Split(":line ");
            var file = traceElements[0].Replace("\\", "/");
            file = file.Remove(0, file.IndexOf("/Assets/") + 1);
            int.TryParse(traceElements[1], out var line);
            LogError($"{o}{method}", file, line, 0);
        }

        private void LogError(object o, string file, int line, int column)
        {
            _messages.Add(new DiagnosticMessage()
            {
                DiagnosticType = DiagnosticType.Error,
                MessageData = o.ToString(),
                File = file,
                Line = line,
                Column = column,
            });
        }
    }
}
