using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    public class TypeReference
    {
        public string Name { get; }
        public TypeInfo TypeInfo { get; }

        public TypeReference(string name, TypeInfo typeInfo)
        {
            Name = name;
            TypeInfo = typeInfo;
        }

        public override string ToString() => Name;
    }
}
