using System;

namespace Katuusagi.ConstExpressionForUnity
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class StaticExpressionAttribute : Attribute
    {
        public bool CalculationFailedWarning { get; set; } = true;
    }
}
