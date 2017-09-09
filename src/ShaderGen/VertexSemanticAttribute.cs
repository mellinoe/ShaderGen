using System;

namespace ShaderGen
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class VertexSemanticAttribute : Attribute
    {
        public SemanticType Type { get; }
        public VertexSemanticAttribute(SemanticType type)
        {
            Type = type;
        }
    }
}
