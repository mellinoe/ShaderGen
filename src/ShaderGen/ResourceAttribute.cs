using System;

namespace ShaderGen
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ResourceAttribute : Attribute
    {
        public int Binding { get; set; }
        public ResourceAttribute(int binding)
        {
            Binding = binding;
        }
    }
}
