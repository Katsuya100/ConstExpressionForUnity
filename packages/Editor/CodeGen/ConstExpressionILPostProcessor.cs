using Katuusagi.ILPostProcessorCommon;
using Katuusagi.ILPostProcessorCommon.Editor;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Katuusagi.ConstExpressionForUnity.Editor
{
    internal class ConstExpressionILPostProcessor : ILPostProcessor
    {
        private Dictionary<string, Delegate> _constExprTable = null;
        private Dictionary<MethodReference, bool> _staticExprCheck = new Dictionary<MethodReference, bool>(MethodReferenceComparer.Default);
        private ConstTableGenerator _constTable = null;
        private StaticTableGenerator _staticTable = null;
        private static TypeReference _readOnlyArrayType;
        private static HashSet<TypeReference> _allowedStaticExpressionTypes = new HashSet<TypeReference>(TypeReferenceComparer.Default);
        private Dictionary<FieldReference, object> _results = new Dictionary<FieldReference, object>(FieldReferenceComparer.Default);

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

            try
            {
                ILPPUtils.InitLog<ConstExpressionILPostProcessor>(compiledAssembly);
                _constExprTable = FindConstExprMethods();
                using (var assembly = ILPPUtils.LoadAssemblyDefinition(compiledAssembly))
                {
                    var mainModule = assembly.MainModule;
                    _allowedStaticExpressionTypes.Add(mainModule.ImportReference(typeof(Type)));
                    _allowedStaticExpressionTypes.Add(mainModule.ImportReference(typeof(MemberInfo)));
                    _allowedStaticExpressionTypes.Add(mainModule.ImportReference(typeof(TypeInfo)));
                    _allowedStaticExpressionTypes.Add(mainModule.ImportReference(typeof(FieldInfo)));
                    _allowedStaticExpressionTypes.Add(mainModule.ImportReference(typeof(PropertyInfo)));
                    _allowedStaticExpressionTypes.Add(mainModule.ImportReference(typeof(MethodInfo)));
                    _allowedStaticExpressionTypes.Add(mainModule.ImportReference(typeof(MethodBase)));
                    _allowedStaticExpressionTypes.Add(mainModule.ImportReference(typeof(EventInfo)));
                    _allowedStaticExpressionTypes.Add(mainModule.ImportReference(typeof(ConstructorInfo)));
                    _readOnlyArrayType = mainModule.ImportReference(typeof(ReadOnlyArray<>));

                    using (_constTable = new ConstTableGenerator(assembly.MainModule, "Katuusagi.ConstExpressionForUnity.Generated", "$$ConstTable"))
                    using (_staticTable = new StaticTableGenerator(assembly.MainModule, "Katuusagi.ConstExpressionForUnity.Generated", "$$StaticTable"))
                    {
                        foreach (var type in assembly.Modules.SelectMany(v => v.Types).GetAllTypes())
                        {
                            if (!type.HasMethods)
                            {
                                continue;
                            }

                            foreach (var method in type.Methods)
                            {
                                var body = method.Body;
                                if (body == null)
                                {
                                    continue;
                                }

                                bool isChanged = false;
                                var ilProcessor = body.GetILProcessor();
                                var instructions = body.Instructions;
                                for (var i = 0; i < instructions.Count; ++i)
                                {
                                    var instruction = instructions[i];
                                    int diff = 0;
                                    isChanged = ConstExpressionProcess(ilProcessor, method, instruction, ref diff) || isChanged;
                                    isChanged = StaticExpressionProcess(ilProcessor, method, instruction, ref diff) || isChanged;
                                    i += diff;
                                }

                                if (!isChanged)
                                {
                                    continue;
                                }

                                ILPPUtils.ResolveInstructionOpCode(body.Instructions);
                            }
                        }
                    }

                    var pe  = new MemoryStream();
                    var pdb = new MemoryStream();
                    var writeParameter = new WriterParameters
                    {
                        SymbolWriterProvider = new PortablePdbWriterProvider(),
                        SymbolStream         = pdb,
                        WriteSymbols         = true
                    };

                    assembly.Write(pe, writeParameter);
                    return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), ILPPUtils.Logger.Messages);
                }
            }
            catch (Exception e)
            {
                ILPPUtils.LogException(e);
            }
            return new ILPostProcessResult(null, ILPPUtils.Logger.Messages);
        }

        private Dictionary<string, Delegate> FindConstExprMethods()
        {
            var result = new Dictionary<string, Delegate>();
            foreach (var method in ILPPUtils.FindMethods<ConstExpressionAttribute>(typeof(ConstExpressionEntry).Assembly))
            {
                if (!CheckConstExpressionError(method))
                {
                    continue;
                }

                var delType = ILPPUtils.GetDelegateType(method.GetParameters().Select(v => v.ParameterType), method.ReturnType);
                var del = Delegate.CreateDelegate(delType, method);
                var args = string.Empty;
                foreach (var parameter in method.GetParameters())
                {
                    args = $"{args}{ILPPUtils.GetTypeName(parameter.ParameterType)},";
                }

                if (!string.IsNullOrEmpty(args))
                {
                    args = args.Remove(args.Length - 1, 1);
                }
                string returnName = ILPPUtils.GetTypeName(method.ReturnType);
                result.Add($"{returnName} {ILPPUtils.GetTypeName(method.ReflectedType)}::{method.Name}({args})", del);
            }

            return result;
        }

        private bool ConstExpressionProcess(ILProcessor ilProcessor, MethodDefinition method, Instruction instruction, ref int instructionDiff)
        {
            if (instruction.OpCode != OpCodes.Call ||
                !_constExprTable.TryGetValue(instruction.Operand.ToString(), out var constExpr))
            {
                return false;
            }

            var parameters = constExpr.Method.GetParameters();
            var args = new object[parameters.Length];
            var argInstruction = instruction;
            if (parameters.Any())
            {
                for (int i = parameters.Length - 1; i >= 0; --i)
                {
                    argInstruction = argInstruction.Previous;
                    if (!TryGetConstValue(ref argInstruction, parameters[i].ParameterType, out var arg))
                    {
                        if (constExpr.Method.GetCustomAttribute<ConstExpressionAttribute>().CalculationFailedWarning)
                        {
                            ILPPUtils.LogWarning("CONSTEXPR0501", "ConstExpression warning.", $"ConstExpression accepts only constants as arguments.", method, instruction);
                        }
                        return false;
                    }

                    args[i] = arg;
                }
            }

            object resultLiteral;
            try
            {
                resultLiteral = constExpr.DynamicInvoke(args);
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException t &&
                    t.InnerException != null)
                {
                    e = t.InnerException;
                }

                ILPPUtils.LogException(e);
                return false;
            }

            var loadLiteral = _constTable.LoadValue(resultLiteral);
            if (loadLiteral.OpCode == OpCodes.Ldsfld &&
                loadLiteral.Operand is FieldReference field &&
                !_results.ContainsKey(field))
            {
                _results.Add(field, resultLiteral);
            }
            ++instructionDiff;
            ilProcessor.InsertAfter(instruction, loadLiteral);

            while (argInstruction != instruction)
            {
                --instructionDiff;
                ILPPUtils.ReplaceTarget(ilProcessor, argInstruction, loadLiteral);
                argInstruction = argInstruction.Next;
                ilProcessor.Remove(argInstruction.Previous);
            }

            --instructionDiff;
            ILPPUtils.ReplaceTarget(ilProcessor, instruction, loadLiteral);
            ilProcessor.Remove(instruction);

            return true;
        }

        private bool TryGetConstValue(ref Instruction instruction, Type type, out object result)
        {
            if (ILPPUtils.TryGetConstValue(ref instruction, type, out result))
            {
                return true;
            }

            var opCode = instruction.OpCode;
            var operand = instruction.Operand;
            if (opCode == OpCodes.Ldsfld && (operand is FieldReference f))
            {
                var ret = _results.TryGetValue(f, out result);
                return ret;
            }

            /*
            if (instruction.OpCode == OpCodes.Ldloc_0)
            {
                var ret = TryGetConstValue(instruction, type, OpCodes.Stloc_0, instruction.Operand, out result);
                return ret;
            }

            if (instruction.OpCode == OpCodes.Ldloc_1)
            {
                var ret = TryGetConstValue(instruction, type, OpCodes.Stloc_1, instruction.Operand, out result);
                return ret;
            }

            if (instruction.OpCode == OpCodes.Ldloc_2)
            {
                var ret = TryGetConstValue(instruction, type, OpCodes.Stloc_2, instruction.Operand, out result);
                return ret;
            }

            if (instruction.OpCode == OpCodes.Ldloc_3)
            {
                var ret = TryGetConstValue(instruction, type, OpCodes.Stloc_3, instruction.Operand, out result);
                return ret;
            }

            if (instruction.OpCode == OpCodes.Ldloc_S)
            {
                var ret = TryGetConstValue(instruction, type, OpCodes.Stloc_S, instruction.Operand, out result);
                return ret;
            }

            if (instruction.OpCode == OpCodes.Ldloc)
            {
                var ret = TryGetConstValue(instruction, type, OpCodes.Stloc, instruction.Operand, out result);
                return ret;
            }
            */

            result = null;
            return false;
        }

        public bool TryGetConstValue(Instruction instruction, Type type, OpCode opCode, object operand, out object result)
        {
            var stloc = instruction.Previous;
            while (stloc.OpCode == opCode && stloc.Operand == operand)
            {
                stloc = stloc.Previous;
                if (stloc == null)
                {
                    result = null;
                    return false;
                }
            }

            var ret = TryGetConstValue(ref stloc, type, out result);
            return ret;
        }

        private bool CheckConstExpressionError(MethodInfo method)
        {
            if (!method.IsStatic)
            {
                ILPPUtils.LogError("CONSTEXPR0001", "ConstExpression failed.", $"ConstExpression is static method only.", method);
                return false;
            }

            if (method.IsGenericMethod ||
                method.ReflectedType.IsGenericType)
            {
                ILPPUtils.LogError("CONSTEXPR0002", "ConstExpression failed.", $"ConstExpression does not support generic argument.", method);
                return false;
            }

            if (!IsAllowConstExpressionType(method.ReturnType))
            {
                ILPPUtils.LogError("CONSTEXPR0003", "ConstExpression failed.", $"ConstExpression does not support return of type \"{method.ReturnType.FullName}\".", method);
                return false;
            }

            foreach (var parameter in method.GetParameters())
            {
                if (IsAllowConstExpressionType(parameter.ParameterType))
                {
                    continue;
                }

                ILPPUtils.LogError("CONSTEXPR0004", "ConstExpression failed.", $"ConstExpression does not support parameters of type \"{parameter.ParameterType.FullName}\".", method);
                return false;
            }

            return true;
        }

        private static bool IsAllowConstExpressionType(Type type)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(ReadOnlyArray<>))
                {
                    var etype = type.GetGenericArguments()[0];
                    return IsAllowConstExpressionType(etype);
                }
            }

            var result = type == typeof(string) ||
                type.IsEnum ||
                ILPPUtils.IsStructRecursive(type);
            return result;
        }

        private bool StaticExpressionProcess(ILProcessor ilProcessor, MethodDefinition method, Instruction instruction, ref int instructionDiff)
        {
            if (method.Is(_staticTable.Constructor))
            {
                return false;
            }

            if (!TryGetStaticExpression(instruction, out var staticExpr))
            {
                return false;
            }

            var attr = staticExpr.Resolve().GetAttribute("Katuusagi.ConstExpressionForUnity.StaticExpressionAttribute");
            var tmp = attr.Properties.FirstOrDefault(v => v.Name == "CalculationFailedWarning").Argument.Value;
            if (!(tmp is bool calculationFailedWarning))
            {
                calculationFailedWarning = true;
            }

            if (staticExpr.ContainsGenericParameter)
            {
                if (calculationFailedWarning)
                {
                    ILPPUtils.LogWarning("CONSTEXPR1502", "ConstExpression warning.", $"StaticExpression cannot use GenericParameter as a type arguments.", method, instruction);
                }
                return false;
            }

            var parameters = staticExpr.Parameters;
            var argInstruction = instruction;
            var argInstructions = new List<Instruction>();
            if (parameters.Any())
            {
                for (int i = 0; i < parameters.Count; ++i)
                {
                    argInstruction = argInstruction.Previous;
                    if (!ILPPUtils.TryGetConstInstructions(ref argInstruction, argInstructions))
                    {
                        if (calculationFailedWarning)
                        {
                            ILPPUtils.LogWarning("CONSTEXPR1501", "ConstExpression warning.", $"StaticExpression accepts only constants as arguments.", method, instruction);
                        }
                        return false;
                    }
                }
            }
            argInstructions.Reverse();

            var loadLiteral = _staticTable.LoadValue(staticExpr, argInstructions);

            ++instructionDiff;
            ilProcessor.InsertAfter(instruction, loadLiteral);

            while (argInstruction != instruction)
            {
                --instructionDiff;
                ILPPUtils.ReplaceTarget(ilProcessor, argInstruction, loadLiteral);
                argInstruction = argInstruction.Next;
                ilProcessor.Remove(argInstruction.Previous);
            }

            --instructionDiff;
            ILPPUtils.ReplaceTarget(ilProcessor, instruction, loadLiteral);
            ilProcessor.Remove(instruction);
            return true;
        }

        private bool TryGetStaticExpression(Instruction instruction, out MethodReference result)
        {
            if (instruction.OpCode != OpCodes.Call)
            {
                result = null;
                return false;
            }

            if (!(instruction.Operand is MethodReference methodRef))
            {
                result = null;
                return false;
            }

            var method = methodRef.Resolve();
            if (method == null)
            {
                result = null;
                return false;
            }

            if (_staticExprCheck.TryGetValue(method, out var check))
            {
                result = methodRef;
                return check;
            }

            if (!method.HasAttribute("Katuusagi.ConstExpressionForUnity.StaticExpressionAttribute"))
            {
                result = null;
                _staticExprCheck.Add(method, false);
                return false;
            }

            if (!CheckStaticExpressionError(method))
            {
                result = null;
                _staticExprCheck.Add(method, false);
                return false;
            }

            result = methodRef;
            _staticExprCheck.Add(method, true);
            return true;
        }

        private bool CheckStaticExpressionError(MethodReference methodRef)
        {
            var method = methodRef.Resolve();
            if (!method.IsStatic)
            {
                ILPPUtils.LogError("CONSTEXPR1001", "ConstExpression failed.", $"StaticExpression is static method only.", method);
                return false;
            }

            if (!method.IsPublic)
            {
                ILPPUtils.LogError("CONSTEXPR1002", "ConstExpression failed.", $"StaticExpression is public method only.", method);
                return false;
            }

            if (!IsAllowStaticExpressionType(methodRef.ReturnType))
            {
                ILPPUtils.LogError("CONSTEXPR1003", "ConstExpression failed.", $"StaticExpression does not support return of type \"{methodRef.ReturnType.FullName}\".", method);
                return false;
            }

            foreach (var parameter in methodRef.Parameters)
            {
                if (IsAllowStaticExpressionType(parameter.ParameterType))
                {
                    continue;
                }

                ILPPUtils.LogError("CONSTEXPR1004", "ConstExpression failed.", $"StaticExpression does not support parameters of type \"{parameter.ParameterType.FullName}\".", method);
                return false;
            }

            return true;
        }

        private static bool IsAllowStaticExpressionType(TypeReference type)
        {
            if (type is GenericInstanceType genType)
            {
                var genDefType = genType.ElementType;
                if (genDefType.Is(_readOnlyArrayType))
                {
                    var etype = genType.GenericArguments[0];
                    return IsAllowStaticExpressionType(etype);
                }
            }

            var result = type.IsString() ||
                         type.IsEnum() ||
                         _allowedStaticExpressionTypes.Contains(type) ||
                         ILPPUtils.IsStructRecursive(type);
            return result;
        }
    }
}
