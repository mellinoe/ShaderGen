using System;

namespace ShaderGen
{
    internal class ShaderSetInfo
    {
        public string Name { get; }
        public TypeAndMethodName VertexShader { get; }
        public TypeAndMethodName FragmentShader { get; }

        public ShaderSetInfo(string name, TypeAndMethodName vs, TypeAndMethodName fs)
        {
            if (vs == null && fs == null)
            {
                throw new ArgumentException("At least one of vs or fs must be non-null.");
            }

            Name = name;
            VertexShader = vs;
            FragmentShader = fs;
        }
    }
}