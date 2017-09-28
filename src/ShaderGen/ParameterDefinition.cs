using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShaderGen
{
    public class ParameterDefinition
    {
        public string Name { get; }
        public TypeReference Type { get; }

        public ParameterDefinition(string name, TypeReference type)
        {
            Name = name;
            Type = type;
        }

        public static ParameterDefinition GetParameterDefinition(Compilation compilation, ParameterSyntax ps)
        {
            string fullType = compilation.GetSemanticModel(ps.SyntaxTree).GetFullTypeName(ps.Type);
            string name = ps.Identifier.ToFullString();
            return new ParameterDefinition(name, new TypeReference(fullType));
        }
    }
}
