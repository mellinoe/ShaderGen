namespace ShaderGen
{
    public class ShaderFunction
    {
        public string Name { get; }
        public TypeReference ReturnType { get; }
        public ParameterDefinition[] Parameters { get; }

        public ShaderFunction(
            string name,
            TypeReference returnType,
            ParameterDefinition[] parameters)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
        }
    }
}
