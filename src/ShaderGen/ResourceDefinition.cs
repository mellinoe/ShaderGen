using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    public class ResourceDefinition
    {
        public string Name { get; }
        public int Set { get; }
        public int Binding { get; }
        public TypeReference ValueType { get; }
        public ShaderResourceKind ResourceKind { get; }

        public ResourceDefinition(string name, int set, int binding, TypeReference valueType, ShaderResourceKind kind)
        {
            Name = name;
            Set = set;
            Binding = binding;
            ValueType = valueType;
            ResourceKind = kind;
        }
    }
}
