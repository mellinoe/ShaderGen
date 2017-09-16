using System;

namespace ShaderGen
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ShaderSetAttribute : Attribute
    {
        public string Name { get; }
        public string VertexShader { get; }
        public string FragmentShader { get; }

        public ShaderSetAttribute(string name, string vs, string fs)
        {
            Name = name;
            VertexShader = vs;
            FragmentShader = fs;
        }
    }
}
