using System;
using System.Text;

namespace TestDynamic
{
    // public enum ValidationType
    // {
    //     NotNull,
    //     GreaterThan
    // }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    public abstract class ValidateAttribute : Attribute
    {
        public abstract void Generate(StringBuilder sb, string name, Type type);
    }

    public class NotNullAttribute : ValidateAttribute
    {
        public override void Generate(StringBuilder sb, string name, Type type)
        {
            sb.Append($"if ({name} == null) throw new ValidationException(\"{name} cannot be null\");");
        }
    }

    public class GreaterThanAttribute : ValidateAttribute
    {
        public double GreaterThanValue { get; set; }
        public GreaterThanAttribute(double greaterThanValue)
        {
            GreaterThanValue = greaterThanValue;
        }

        public override void Generate(StringBuilder sb, string name, Type type)
        {
            sb.Append($"if ({name} <= {GreaterThanValue}) throw new ValidationException(\"{name} must be greater than {GreaterThanValue}\");");
        }
    }
}
