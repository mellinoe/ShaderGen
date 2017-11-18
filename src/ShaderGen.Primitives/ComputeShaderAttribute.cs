using System;

namespace ShaderGen
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ComputeShaderAttribute : Attribute
    {
        public uint GroupCountX { get; }
        public uint GroupCountY { get; }
        public uint GroupCountZ { get; }

        public ComputeShaderAttribute(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            GroupCountX = groupCountX;
            GroupCountY = groupCountY;
            GroupCountZ = groupCountZ;
        }
    }
}
