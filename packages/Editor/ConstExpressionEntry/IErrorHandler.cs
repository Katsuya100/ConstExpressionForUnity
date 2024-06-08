using Mono.Cecil.Cil;
using Mono.Cecil;
using System.Collections.Generic;

namespace Katuusagi.ConstExpressionForUnity.Editor
{
    public interface IErrorHandler
    {
        void Check(MethodDefinition method, Instruction instruction, MethodReference called, IReadOnlyList<Instruction> argInstructions, IReadOnlyDictionary<string, object> parameters);
    }
}
