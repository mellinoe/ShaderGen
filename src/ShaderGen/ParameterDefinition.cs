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
    }
}
