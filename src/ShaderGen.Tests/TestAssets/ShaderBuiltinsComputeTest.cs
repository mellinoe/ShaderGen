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
        /// <summary>
        /// The number of methods.
        /// </summary>
        public const uint Methods = 4;

        [ResourceSet(0)] public RWStructuredBuffer<ComputeShaderParameters> InOutParameters;

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
            int index = (int)dispatchThreadID.X;

            // ReSharper disable once RedundantCast - WORKAROUND for #75
            if (index >= Methods) return;

            ComputeShaderParameters parameters = InOutParameters[index];
            switch (index)
            {
                // Abs
                case 0:
                    parameters.OutFloat = Abs(parameters.P1Float);
                    break;
                case 1:
                    parameters.OutVector2 = Abs(parameters.P1Vector2);
                    break;
                case 2:
                    parameters.OutVector3 = Abs(parameters.P1Vector3);
                    break;
                case 3:
                    parameters.OutVector4 = Abs(parameters.P1Vector4);
                    break;
            }

            InOutParameters[index] = parameters;
        }
    }
}