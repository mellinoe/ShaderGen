using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShaderGen
{
    public class ParameterDefinition
    {
        public string Name { get; }
        public TypeReference Type { get; }
        public ParameterDirection Direction { get; }
        public IParameterSymbol Symbol { get; }

        public ParameterDefinition(string name, TypeReference type, ParameterDirection direction, IParameterSymbol symbol)
        {
            Name = name;
            Type = type;
            Direction = direction;
            Symbol = symbol;
        }

        public static ParameterDefinition GetParameterDefinition(Compilation compilation, ParameterSyntax ps)
        {
            SemanticModel semanticModel = compilation.GetSemanticModel(ps.SyntaxTree);

            string fullType = semanticModel.GetFullTypeName(ps.Type);
            string name = ps.Identifier.ToFullString();

            ParameterDirection direction = ParameterDirection.In;

            IParameterSymbol declaredSymbol = (IParameterSymbol)semanticModel.GetDeclaredSymbol(ps);
            RefKind refKind = declaredSymbol.RefKind;
            if (refKind == RefKind.Out)
            {
                direction = ParameterDirection.Out;
            }
            else if (refKind == RefKind.Ref)
            {
                direction = ParameterDirection.InOut;
            }

            return new ParameterDefinition(name, new TypeReference(fullType, semanticModel.GetTypeInfo(ps.Type).Type), direction, declaredSymbol);
        }
    }

    public enum ParameterDirection
    {
        In,
        Out,
        InOut
    }
}
