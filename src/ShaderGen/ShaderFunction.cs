using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShaderGen
{
    public class ShaderFunction
    {
        public string Name { get; }
        public TypeReference ReturnType { get; }
        public ParameterDefinition[] Parameters { get; }
        public BlockSyntax BlockSyntax { get; }

        public ShaderFunction(
            string name,
            TypeReference returnType,
            ParameterDefinition[] parameters,
            BlockSyntax blockSyntax)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
            BlockSyntax = blockSyntax;
        }
    }
}
