using Katuusagi.ILPostProcessorCommon;
using Katuusagi.ILPostProcessorCommon.Editor;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Katuusagi.ConstExpressionForUnity.Editor
{
    internal class ConstExpressionILPostProcessor : ILPostProcessor
    {
        private Dictionary<string, Delegate> _constExprTable = null;
        private ConstTableGenerator _constTable = null;
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

            ILPostProcessorUtils.Logger = new Logger();

            try
            {
                _constExprTable = FindConstExprMethods();
                using (var assembly = ILPostProcessorUtils.LoadAssemblyDefinition(compiledAssembly))
                {
                    using (_constTable = new ConstTableGenerator(assembly.MainModule, "Katuusagi.ConstExpression.Generated", "$$ConstTable"))
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
                                    isChanged = ConstExpressionProcess(ilProcessor, method, instruction, out var diff) || isChanged;
                                    i += diff;
                                }

                                if (!isChanged)
                                {
                                    continue;
                                }

                                ILPostProcessorUtils.ResolveInstructionOpCode(body.Instructions);
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
                    return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), ILPostProcessorUtils.Logger.Messages);
                }
            }
            catch (Exception e)
            {
                ILPostProcessorUtils.LogError(e);
            }
            return new ILPostProcessResult(null, ILPostProcessorUtils.Logger.Messages);
        }

        private bool ConstExpressionProcess(ILProcessor ilProcessor, MethodDefinition method, Instruction instruction, out int instructionDiff)
        {
            instructionDiff = 0;
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
                    if (!ILPostProcessorUtils.TryEmulateLiteral(ref argInstruction, parameters[i].ParameterType, out var arg))
                    {
                        if (constExpr.Method.GetCustomAttribute<ConstExpressionAttribute>().CalculationFailedWarning)
                        {
                            ILPostProcessorUtils.LogWarning($"ConstExpression calculation failed.", method, argInstruction);
                        }
                        return false;
                    }

                    args[i] = arg;
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

                ILPostProcessorUtils.LogError($"{e.GetType()}:{e.Message}", e.StackTrace);
                return false;
            }

            var loadLiteral = _constTable.LoadValue(literal);
            ++instructionDiff;
            ilProcessor.InsertAfter(instruction, loadLiteral);

            while (argInstruction != instruction)
            {
                --instructionDiff;
                ILPostProcessorUtils.ReplaceTarget(ilProcessor, argInstruction, loadLiteral);
                argInstruction = argInstruction.Next;
                ilProcessor.Remove(argInstruction.Previous);
            }

            --instructionDiff;
            ILPostProcessorUtils.ReplaceTarget(ilProcessor, instruction, loadLiteral);
            ilProcessor.Remove(instruction);

            return true;
        }

        private Dictionary<string, Delegate> FindConstExprMethods()
        {
            var result = new Dictionary<string, Delegate>();
            foreach (var method in ILPostProcessorUtils.FindMethods<ConstExpressionAttribute>(typeof(ConstExpressionEntry).Assembly))
            {
                if (!CheckMethodError(method))
                {
                    continue;
                }

                var delType = ILPostProcessorUtils.GetDelegateType(method.GetParameters().Select(v => v.ParameterType), method.ReturnType);
                var del = Delegate.CreateDelegate(delType, method);
                var args = string.Empty;
                foreach (var parameter in method.GetParameters())
                {
                    args = $"{args}{ILPostProcessorUtils.GetTypeName(parameter.ParameterType)},";
                }

                if (!string.IsNullOrEmpty(args))
                {
                    args = args.Remove(args.Length - 1, 1);
                }
                string returnName = ILPostProcessorUtils.GetTypeName(method.ReturnType);
                result.Add($"{returnName} {method.ReflectedType.FullName}::{method.Name}({args})", del);
            }

            return result;
        }

        private bool CheckMethodError(MethodInfo method)
        {
            if (!method.IsStatic)
            {
                ILPostProcessorUtils.LogError($"ConstExpression is static method only.", method);
                return false;
            }

            if (method.IsGenericMethod ||
                method.ReflectedType.IsGenericType)
            {
                ILPostProcessorUtils.LogError($"ConstExpression does not support generic argument.", method);
                return false;
            }

            if (!IsAllowConstExpressionReturnType(method.ReturnType))
            {
                ILPostProcessorUtils.LogError($"ConstExpression does not support return of type \"{method.ReturnType.FullName}\".", method);
                return false;
            }

            foreach (var parameter in method.GetParameters())
            {
                if (IsAllowConstExpressionParameterType(parameter.ParameterType))
                {
                    continue;
                }

                ILPostProcessorUtils.LogError($"ConstExpression does not support parameters of type \"{method.ReturnType.FullName}\".", method);
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

            if (!ILPostProcessorUtils.IsStructRecursive(type))
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
    }
}
