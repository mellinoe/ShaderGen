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

        /// <summary>
        /// True if this resource is a texture type is passed to a comparison-sampling function.
        /// Needed by the "legacy" GLSL backends.
        /// </summary>
        public bool IsTextureUsedAsDepthTexture { get; internal set; }

        /// <summary>
        /// If this is a texture, stores all the parameters that this texture is passed as an argument to.
        /// Needed by the "legacy" GLSL backends.
        /// </summary>
        public List<IParameterSymbol> ParameterSymbols { get; } = new List<IParameterSymbol>();

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
