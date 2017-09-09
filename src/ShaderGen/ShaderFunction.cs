using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShaderGen
{
    public class ShaderFunction
    {
        public string Name { get; }
        public TypeReference ReturnType { get; }
        public ParameterDefinition[] Parameters { get; }
        public bool IsEntryPoint { get; }
        public BlockSyntax BlockSyntax { get; }

        public ShaderFunction(
            string name,
            TypeReference returnType,
            ParameterDefinition[] parameters,
            bool isEntryPoint,
            BlockSyntax blockSyntax)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
            IsEntryPoint = isEntryPoint;
            BlockSyntax = blockSyntax;
        }

        public ShaderFunction WithReturnType(TypeReference returnType)
        {
            return new ShaderFunction(Name, returnType, Parameters, IsEntryPoint, BlockSyntax);
        }
    }
}
