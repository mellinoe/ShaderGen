using System;

namespace ShaderGen
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ComputeShaderSetAttribute : Attribute
    {
        public ComputeShaderSetAttribute(string setName, string computeShaderFunctionName)
        {
        }
    }
}
