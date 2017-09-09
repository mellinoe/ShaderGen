using System;

namespace ShaderGen
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SemanticTypeAttribute : Attribute
    {
        public SemanticType Type { get; }
        public SemanticTypeAttribute(SemanticType type)
        {
            Type = type;
        }
    }
}
