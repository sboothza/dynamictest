using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using HarmonyLib;

namespace TestDynamic
{
    [Serializable]
    public class ValidationException : Exception
    {
        public ValidationException() { }
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception inner) : base(message, inner) { }
        protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public struct ParameterInfo
    {
        public string Name;
        public Type Type;
        public List<ValidateAttribute> Validations;

        public ParameterInfo(string name, Type type)
        {
            Name = name;
            Type = type;
            Validations = new List<ValidateAttribute>();
        }

        public string GenerateMethodParameter()
        {
            return $"{Type.Name} {Name}";
        }

        public void GenerateValidationChecks(StringBuilder sb)
        {
            foreach (var attrib in Validations)
            {
                attrib.Generate(sb, Name, Type);
            }
        }
    }

    public class ValidationSignature
    {
        public string Name { get; set; }
        public MethodInfo OriginalMethod { get; set; }
        public MethodInfo ValidateMethod { get; set; }
        public List<ParameterInfo> Parameters { get; }

        public ValidationSignature(string name)
        {
            Name = name;
            Parameters = new List<ParameterInfo>();
        }

        private void GenerateMethodParameters(StringBuilder sb)
        {
            sb.Append('(');
            sb.Append(string.Join(", ", Parameters.Select(p => p.GenerateMethodParameter())));
            sb.Append(')');
        }

        public void Generate(StringBuilder sb)
        {
            sb.Append($"public static bool {Name}");
            GenerateMethodParameters(sb);
            sb.Append('{');

            foreach (var info in Parameters)
            {
                info.GenerateValidationChecks(sb);
            }

            sb.Append("return true;");

            sb.Append('}');
        }
    }

    public class Validations
    {
        private readonly List<ValidationSignature> _signatures;
        public string Name { get; set; }
        public Assembly Assembly { get; set; }

        public Validations(string name)
        {
            Name = name;
            _signatures = new List<ValidationSignature>();
        }

        public void Add(ValidationSignature signature)
        {
            _signatures.Add(signature);
        }

        public string Generate()
        {
            var sb = new StringBuilder(1024);
            sb.Append($"using System; using TestDynamic; public class {Name} {{");
            foreach (var signature in _signatures)
            {
                signature.Generate(sb);
            }
            sb.Append('}');
            return sb.ToString();
        }

        public void Init(Harmony harmony)
        {
            const BindingFlags All = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            var methods = AppDomain.CurrentDomain
                                       .GetAssemblies()
                                       .SelectMany(a => a.GetTypes())
                                       .SelectMany(t => t.GetMethods(All))
                                       .Where(m => m.GetParameters()
                                                    .SelectMany(p => p.GetCustomAttributes<ValidateAttribute>())
                                                    .Any());
            foreach (var method in methods)
            {
                var parameters = method.GetParameters().ToList();
                var signature = new ValidationSignature($"{method.DeclaringType.Name}_{method.Name}_Validate");
                signature.OriginalMethod = method;
                Add(signature);

                foreach (var parameter in parameters)
                {
                    var attribs = parameter.GetCustomAttributes<ValidateAttribute>();
                    if (attribs.Any())
                    {
                        var parm = new ParameterInfo(parameter.Name, parameter.ParameterType);
                        parm.Validations.AddRange(attribs);
                        signature.Parameters.Add(parm);
                    }
                }
            }

            var code = Generate();
            Assembly = Dynamic.BuildFromCode(code);

            var validatorType = Assembly.GetType(Name);
            foreach (var signature in _signatures)
            {
                var method = validatorType.GetMethod(signature.Name);
                signature.ValidateMethod = method;
                harmony.Patch(signature.OriginalMethod, new HarmonyMethod(method));
            }
        }
    }
}
