namespace ShaderGen
{
    public class ResourceDefinition
    {
        public string Name { get; }
        public int Binding { get; }
        public TypeReference ValueType { get; }
        /// <summary>
        /// The number of elements in an array resource. Returns 0 if this resource is not an array.
        /// </summary>
        public int ArrayElementCount { get; }
        public bool IsArray => ArrayElementCount > 0;
        public ShaderResourceKind ResourceKind { get; }

        public ResourceDefinition(string name, int binding, TypeReference type, int arrayElementCount, ShaderResourceKind kind)
        {
            Name = name;
            Binding = binding;
            ValueType = type;
            ArrayElementCount = arrayElementCount;
            ResourceKind = kind;
        }
    }
}
