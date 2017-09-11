namespace ShaderGen
{
    public class ResourceDefinition
    {
        public string Name { get; }
        public int Binding { get; }
        public TypeReference ValueType { get; }
        public ShaderResourceKind ResourceKind { get; }

        public ResourceDefinition(string name, int binding, TypeReference type, ShaderResourceKind kind)
        {
            Name = name;
            Binding = binding;
            ValueType = type;
            ResourceKind = kind;
        }
    }
}
