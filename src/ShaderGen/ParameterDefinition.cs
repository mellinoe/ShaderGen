using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShaderGen
{
    public class ParameterDefinition
    {
        public string Name { get; }
        public TypeReference Type { get; }
        public ParameterDirection Direction { get; }

        public ParameterDefinition(string name, TypeReference type, ParameterDirection direction)
        {
            Name = name;
            Type = type;
            Direction = direction;
        }

        public static ParameterDefinition GetParameterDefinition(Compilation compilation, ParameterSyntax ps)
        {
            SemanticModel semanticModel = compilation.GetSemanticModel(ps.SyntaxTree);

            string fullType = semanticModel.GetFullTypeName(ps.Type);
            string name = ps.Identifier.ToFullString();

            ParameterDirection direction = ParameterDirection.In;

            IParameterSymbol declaredSymbol = (IParameterSymbol) semanticModel.GetDeclaredSymbol(ps);
            RefKind refKind = declaredSymbol.RefKind;
            if (refKind == RefKind.Out)
            {
                direction = ParameterDirection.Out;
            }
            else if (refKind == RefKind.Ref)
            {
                direction = ParameterDirection.InOut;
            }

            return new ParameterDefinition(name, new TypeReference(fullType), direction);
        }
    }

    public enum ParameterDirection
    {
        In,
        Out,
        InOut
    }
}
