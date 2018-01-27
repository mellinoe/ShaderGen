using System;

namespace ShaderGen
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ComputeShaderSetAttribute : Attribute
    {
        public string SetName { get; set; }
        public string ComputeShaderFunctionName { get; set; }
        
        public ComputeShaderSetAttribute(string setName, string computeShaderFunctionName) {
            SetName = setName;
            ComputeShaderFunctionName = computeShaderFunctionName;
        }
    }
}
