using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShaderGen
{
    public class ShaderFunction
    {
        public string Name { get; }
        public TypeReference ReturnType { get; }
        public ParameterDefinition[] Parameters { get; }
        public bool IsEntryPoint { get; }

        public ShaderFunction(
            string name,
            TypeReference returnType,
            ParameterDefinition[] parameters,
            bool isEntryPoint)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
            IsEntryPoint = isEntryPoint;
        }

        public ShaderFunction WithReturnType(TypeReference returnType)
        {
            return new ShaderFunction(Name, returnType, Parameters, IsEntryPoint);
        }
    }
}
