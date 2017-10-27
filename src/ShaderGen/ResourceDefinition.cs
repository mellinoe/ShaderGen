namespace ShaderGen
{
    public class ResourceDefinition
    {
        public string Name { get; }
        public int Set { get; }
        public int Binding { get; }
        public TypeReference ValueType { get; }
        public ShaderResourceKind ResourceKind { get; }

        public ResourceDefinition(string name, int set, int binding, TypeReference type, ShaderResourceKind kind)
        {
            Name = name;
            Set = set;
            Binding = binding;
            ValueType = type;
            ResourceKind = kind;
        }
    }
}
