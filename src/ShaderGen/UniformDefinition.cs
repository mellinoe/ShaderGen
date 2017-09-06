namespace ShaderGen
{
    public class UniformDefinition
    {
        public string Name { get; }
        public int Binding { get; }
        public TypeReference Type { get; }

        public UniformDefinition(string name, int binding, TypeReference type)
        {
            Name = name;
            Binding = binding;
            Type = type;
        }
    }
}
