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
        private Dictionary<string, Delegate> _constExprTable = new Dictionary<string, Delegate>();
        private Dictionary<string, IErrorHandler> _constExprErrorHandlerTable = new Dictionary<string, IErrorHandler>();
        private Dictionary<MethodReference, bool> _staticExprCheck = new Dictionary<MethodReference, bool>(MethodReferenceComparer.Default);
        private ConstTableGenerator _constTable = null;
        private StaticTableGenerator _staticTable = null;
        private HashSet<TypeReference> _allowedStaticExpressionTypes = new HashSet<TypeReference>(TypeReferenceComparer.Default);
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
                FindConstExprMethods(_constExprTable);
                using (var assembly = ILPPUtils.LoadAssemblyDefinition(compiledAssembly))
                {
                    var ignoreConstExpressionAssembly = assembly.CustomAttributes.Any(v => v.AttributeType.FullName == "Katuusagi.ConstExpressionForUnity.IgnoreConstExpressionAttribute");
                    var ignoreStaticExpressionAssembly = assembly.CustomAttributes.Any(v => v.AttributeType.FullName == "Katuusagi.ConstExpressionForUnity.IgnoreStaticExpressionAttribute");
                    if (!ignoreConstExpressionAssembly || !ignoreStaticExpressionAssembly)
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
                        _allowedStaticExpressionTypes.Add(mainModule.TypeSystem.String);

                        using (_constTable = new ConstTableGenerator(assembly.MainModule, "Katuusagi.ConstExpressionForUnity.Generated", "$$ConstTable"))
                        using (_staticTable = new StaticTableGenerator(assembly.MainModule, "Katuusagi.ConstExpressionForUnity.Generated", "$$StaticTable"))
                        {
                            using (ThreadStaticArrayPool.Get(out var types, assembly.Modules.SelectMany(v => v.Types).GetAllTypes()))
                            {
                                foreach (var type in types)
                                {
                                    if (!type.HasMethods)
                                    {
                                        continue;
                                    }

                                    var ignoreConstExpressionType = ignoreConstExpressionAssembly || type.CustomAttributes.Any(v => v.AttributeType.FullName == "Katuusagi.ConstExpressionForUnity.IgnoreConstExpressionAttribute");
                                    var ignoreStaticExpressionType = ignoreStaticExpressionAssembly || type.CustomAttributes.Any(v => v.AttributeType.FullName == "Katuusagi.ConstExpressionForUnity.IgnoreStaticExpressionAttribute");
                                    if (ignoreConstExpressionType && ignoreStaticExpressionType)
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

                                        var ignoreConstExpressionMethod = ignoreConstExpressionType || method.CustomAttributes.Any(v => v.AttributeType.FullName == "Katuusagi.ConstExpressionForUnity.IgnoreConstExpressionAttribute");
                                        var ignoreStaticExpressionMethod = ignoreStaticExpressionType || method.CustomAttributes.Any(v => v.AttributeType.FullName == "Katuusagi.ConstExpressionForUnity.IgnoreStaticExpressionAttribute");
                                        if (ignoreConstExpressionMethod && ignoreStaticExpressionMethod)
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
                                            if (!ignoreConstExpressionMethod)
                                            {
                                                isChanged = ConstExpressionProcess(ilProcessor, method, instruction, ref diff) || isChanged;
                                            }
                                            if (!ignoreStaticExpressionMethod)
                                            {
                                                isChanged = StaticExpressionProcess(ilProcessor, method, instruction, ref diff) || isChanged;
                                            }
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
            }
            catch (Exception e)
            {
                ILPPUtils.LogException(e);
            }
            return new ILPostProcessResult(null, ILPPUtils.Logger.Messages);
        }

        private void FindConstExprMethods(Dictionary<string, Delegate> result)
        {
            var runtimeMethods = ILPPUtils.FindMethods<ConstExpressionAttribute>(typeof(ConstExpressionForUnity.ConstExpressionEntry).Assembly);
            var editorMethods = ILPPUtils.FindMethods<ConstExpressionAttribute>(typeof(ConstExpressionEntry).Assembly);
            foreach (var method in runtimeMethods.Concat(editorMethods))
            {
                if (!CheckConstExpressionError(method))
                {
                    continue;
                }

                var delType = ILPPUtils.GetDelegateType(method.GetParameters().Select(v => v.ParameterType), method.ReturnType);
                var del = Delegate.CreateDelegate(delType, method);
                var args = string.Join(",", method.GetParameters().Select(v => ILPPUtils.GetTypeName(v.ParameterType)));
                string returnName = ILPPUtils.GetTypeName(method.ReturnType);
                result.Add($"{returnName} {ILPPUtils.GetTypeName(method.ReflectedType)}::{method.Name}({args})", del);
            }
        }

        private bool ConstExpressionProcess(ILProcessor ilProcessor, MethodDefinition method, Instruction instruction, ref int instructionDiff)
        {
            if (instruction.OpCode != OpCodes.Call ||
                !_constExprTable.TryGetValue(instruction.Operand.ToString(), out var constExpr))
            {
                return false;
            }

            var constExprAttr = constExpr.Method.GetCustomAttribute<ConstExpressionAttribute>();
            var parameters = constExpr.Method.GetParameters();
            using (ThreadStaticArrayPool.Get<object>(out var args, parameters.Length))
            using (ThreadStaticDictionaryPool.Get<string, object>(out var parameterTable))
            using (ThreadStaticListPool.Get<Instruction>(out var argInstructions))
            {
                for (int i = 0; i < parameters.Length; ++i)
                {
                    if (!instruction.TryGetPushConstArgumentInstructions(i, out var value, argInstructions) ||
                        (value is FieldReference f && !_results.TryGetValue(f, out value)) ||
                        !ILPPUtils.TryCast(parameters[i].ParameterType, value, out value))
                    {
                        if (constExprAttr.CalculationFailedWarning)
                        {
                            ILPPUtils.LogWarning("CONSTEXPR0501", "ConstExpression warning.", $"ConstExpression accepts only constants as arguments.", method, instruction);
                        }
                        return false;
                    }

                    parameterTable.Add(parameters[i].Name, value);
                    args[i] = value;
                }

                using (ThreadStaticHashSetPool.Get<Instruction>(out var jumpTargets))
                {
                    method.Body.GetJumpTargets(jumpTargets);
                    foreach (var argInstruction in argInstructions.Skip(1).Append(instruction))
                    {
                        if (jumpTargets.Contains(argInstruction))
                        {
                            if (constExprAttr.CalculationFailedWarning)
                            {
                                ILPPUtils.LogWarning("CONSTEXPR0501", "ConstExpression warning.", $"ConstExpression accepts only constants as arguments.", method, instruction);
                            }
                            return false;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(constExprAttr.ErrorHandler))
                {
                    if (TryGetErrorHandler(constExprAttr.ErrorHandler, out var errorHandler))
                    {
                        errorHandler.Check(method, instruction, instruction.Operand as MethodReference, argInstructions, parameterTable);
                    }
                    else
                    {
                        ILPPUtils.LogWarning("CONSTEXPR0502", "ConstExpression warning.", $"Invalid ErrorHandler type \"{constExprAttr.ErrorHandler}\".", method, instruction);
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

                if (!IsAllowConstExpressionReturnValue(resultLiteral))
                {
                    ILPPUtils.LogWarning("CONSTEXPR0503", "ConstExpression warning.", $"ConstExpression does not supported result value \"{resultLiteral}\".", method, instruction);
                    return false;
                }

                var loadLiteral = _constTable.LoadValue(constExpr.Method.ReturnType, resultLiteral);
                if (loadLiteral.OpCode == OpCodes.Ldsfld &&
                    loadLiteral.Operand is FieldReference field &&
                    !_results.ContainsKey(field))
                {
                    _results.Add(field, resultLiteral);
                }

                instruction.OpCode = loadLiteral.OpCode;
                instruction.Operand = loadLiteral.Operand;

                foreach (var argInstruction in argInstructions)
                {
                    argInstruction.OpCode = OpCodes.Nop;
                    argInstruction.Operand = null;
                }
            }

            return true;
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
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyArray<>))
            {
                var etype = type.GetGenericArguments()[0];
                return IsAllowConstExpressionType(etype);
            }

            return !type.IsValueType || type.IsStructRecursive();
        }

        private static bool IsAllowConstExpressionReturnValue(object value)
        {
            if (value == null)
            {
                return true;
            }

            var type = value.GetType();
            var result = type.IsStructRecursive() ||
                         (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyArray<>)) ||
                         type == typeof(string);
            return result;
        }

        private bool StaticExpressionProcess(ILProcessor ilProcessor, MethodDefinition method, Instruction instruction, ref int instructionDiff)
        {
            if (_staticTable.IsStaticTableConstructor(method))
            {
                return false;
            }

            if (!TryGetStaticExpression(instruction, out var staticExpr))
            {
                return false;
            }

            var staticExprDef = staticExpr.Resolve();
            var attr = staticExprDef.GetAttribute("Katuusagi.ConstExpressionForUnity.StaticExpressionAttribute");
            var tmp = attr.Properties.FirstOrDefault(v => v.Name == "CalculationFailedWarning").Argument.Value;
            if (!(tmp is bool calculationFailedWarning))
            {
                calculationFailedWarning = true;
            }

            var errorHandlerName = attr.Properties.FirstOrDefault(v => v.Name == "ErrorHandler").Argument.Value as string;

            var parameters = staticExprDef.Parameters;
            using (ThreadStaticDictionaryPool.Get<string, object>(out var parameterTable))
            using (ThreadStaticListPool.Get<Instruction>(out var argInstructions))
            {
                for (int i = 0; i < parameters.Count; ++i)
                {
                    if (!instruction.TryGetPushConstArgumentInstructions(i, out var value, argInstructions))
                    {
                        if (calculationFailedWarning)
                        {
                            ILPPUtils.LogWarning("CONSTEXPR1501", "ConstExpression warning.", $"StaticExpression accepts only constants as arguments.", method, instruction);
                        }
                        return false;
                    }

                    parameterTable.Add(parameters[i].Name, value);
                }

                using (ThreadStaticHashSetPool.Get<Instruction>(out var jumpTargets))
                {
                    method.Body.GetJumpTargets(jumpTargets);
                    foreach (var argInstruction in argInstructions.Skip(1).Append(instruction))
                    {
                        if (jumpTargets.Contains(argInstruction))
                        {
                            if (calculationFailedWarning)
                            {
                                ILPPUtils.LogWarning("CONSTEXPR1501", "ConstExpression warning.", $"StaticExpression accepts only constants as arguments.", method, instruction);
                            }
                            return false;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(errorHandlerName))
                {
                    if (TryGetErrorHandler(errorHandlerName, out var errorHandler))
                    {
                        errorHandler.Check(method, instruction, instruction.Operand as MethodReference, argInstructions, parameterTable);
                    }
                    else
                    {
                        ILPPUtils.LogWarning("CONSTEXPR0502", "ConstExpression warning.", $"Invalid ErrorHandler type \"{errorHandlerName}\".", method, instruction);
                    }
                }

                var loadLiteral = _staticTable.LoadValue(staticExpr, argInstructions);
                instruction.OpCode = loadLiteral.OpCode;
                instruction.Operand = loadLiteral.Operand;
                foreach (var argInstruction in argInstructions)
                {
                    argInstruction.OpCode = OpCodes.Nop;
                    argInstruction.Operand = null;
                }
            }
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

            if (!IsAllowStaticExpressionReturnType(methodRef.ReturnType))
            {
                ILPPUtils.LogError("CONSTEXPR1003", "ConstExpression failed.", $"StaticExpression does not support return of type \"{methodRef.ReturnType.FullName}\".", method);
                return false;
            }

            foreach (var parameter in methodRef.Parameters)
            {
                if (IsAllowStaticExpressionParameterType(parameter.ParameterType))
                {
                    continue;
                }

                ILPPUtils.LogError("CONSTEXPR1004", "ConstExpression failed.", $"StaticExpression does not support parameters of type \"{parameter.ParameterType.FullName}\".", method);
                return false;
            }

            return true;
        }

        private bool IsAllowStaticExpressionParameterType(TypeReference type)
        {
            var typeDef = type.Resolve();
            if (typeDef == null)
            {
                return false;
            }

            if (typeDef.IsValueType)
            {
                return IsAllowStaticExpressionReturnType(type);
            }

            return true;
        }

        private bool IsAllowStaticExpressionReturnType(TypeReference type)
        {
            if (type.IsPrimitive)
            {
                return true;
            }

            if (type is GenericInstanceType genType)
            {
                var genDefType = genType.ElementType;
                if (genDefType.FullName == "Katuusagi.ILPostProcessorCommon.ReadOnlyArray`1")
                {
                    var etype = genType.GenericArguments[0];
                    return IsAllowStaticExpressionReturnType(etype);
                }
            }

            var typeDef = type.Resolve();
            if (typeDef == null)
            {
                return false;
            }

            if (typeDef.IsEnum)
            {
                return true;
            }

            if (typeDef.IsValueType)
            {
                var fields = type.GetFields().Where(v => !v.IsStatic);
                return fields.Select(v => v.FieldType).All(IsAllowStaticExpressionReturnType);
            }

            if (_allowedStaticExpressionTypes.Contains(type))
            {
                return true;
            }

            return false;
        }

        private bool TryGetErrorHandler(string name, out IErrorHandler errorHandler)
        {
            if (_constExprErrorHandlerTable.TryGetValue(name, out errorHandler))
            {
                return true;
            }

            var type = typeof(ConstExpressionEntry).Assembly.GetType(name);
            if (type == null || type.IsAbstract || type.IsInterface ||
                !typeof(IErrorHandler).IsAssignableFrom(type))
            {
                return false;
            }

            try
            {
                errorHandler = Activator.CreateInstance(type) as IErrorHandler;
            }
            catch
            {
                return false;
            }

            _constExprErrorHandlerTable.Add(name, errorHandler);
            return true;
        }
    }
}
