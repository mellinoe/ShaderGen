using System.Runtime.InteropServices;
using ShaderGen;
using TestShaders;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    /// <summary>
    /// Shader to test built in methods.
    /// </summary>
    public class ShaderBuiltinsComputeTest
    {
        public const uint Methods = 2;

        [ResourceSet(0)] public RWStructuredBuffer<ComputeShaderParameters> InOutParameters;

        [ResourceSet(1)] public uint Method;

        [ComputeShader(1, 1, 1)]
        public void CS()
        {
            DoCS(DispatchThreadID);
        }

        /*
         * TODO Issue #67 - WORKAROUND until DispatchThreadID is removed and the parameter style implemented
         */
        public void DoCS(UInt3 dispatchThreadID)
        {
            uint index = dispatchThreadID.X;

            // ReSharper disable once RedundantCast - WORKAROUND for #75
            if (index >= (uint)Methods) return;

            ComputeShaderParameters parameters = InOutParameters[index];
            switch (dispatchThreadID.X)
            {
                // Abs
                case 0:
                    parameters.OutFloat = Abs(parameters.P1Float);
                    parameters.OutFloatSet = true;
                    break;
            }

            InOutParameters[index] = parameters;
        }
    }
}