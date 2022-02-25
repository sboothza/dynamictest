using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TestDynamic
{
    [Serializable]
    public class BuildException : Exception
    {
        private List<Diagnostic> _messages = new();
        public BuildException() { }
        public BuildException(string message) : base(message) { }
        public BuildException(string message, Exception inner) : base(message, inner) { }

        public BuildException(List<Diagnostic> messages) : base(BuildMessage(messages))
        {
            _messages = messages;
        }

        protected BuildException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        private static string BuildMessage(IEnumerable<Diagnostic> messages)
        {
            var builder = new StringBuilder(1024);
            foreach (var diagnostic in messages.OrderBy(o => o.Location
                                                              .GetLineSpan()
                                                              .StartLinePosition
                                                              .Line))
                builder.AppendLine($"({diagnostic.Location.GetLineSpan().StartLinePosition.Line}) {diagnostic.Id}: {diagnostic.GetMessage()}");

            return builder.ToString();
        }
    }

    public class Dynamic
    {
        private static IEnumerable<PortableExecutableReference> GetStandardReferences()
        {
            var assemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");

            return assemblies?.Split(Path.PathSeparator)
                             .Select(reference => MetadataReference.CreateFromFile(reference))
                             .ToList();
        }

        public static Assembly BuildFromCode(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create(Guid.NewGuid().ToString(),
                                                       new[] { syntaxTree },
                                                       GetStandardReferences(),
                                                       new CSharpCompilationOptions(OutputKind
                                                                                        .DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (result.Success)
                    return Assembly.Load(ms.ToArray());

                throw new BuildException(result.Diagnostics
                                               .Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                                               .ToList());
            }
        }
    }
}
