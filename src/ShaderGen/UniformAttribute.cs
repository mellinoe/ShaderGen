using System;

namespace ShaderGen
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class UniformAttribute : Attribute
    {
        public int Binding { get; set; }

        public UniformAttribute(int binding)
        {
            Binding = binding;
        }
    }
}