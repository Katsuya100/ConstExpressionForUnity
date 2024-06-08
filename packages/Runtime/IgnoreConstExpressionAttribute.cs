using System;

namespace Katuusagi.ConstExpressionForUnity
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class IgnoreConstExpressionAttribute : Attribute
    {
    }
}
