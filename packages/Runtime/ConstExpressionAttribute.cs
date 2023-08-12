using System;

namespace Katuusagi.ConstExpressionForUnity
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ConstExpressionAttribute : Attribute
    {
        public bool CalculationFailedWarning { get; set; } = true;
    }
}
