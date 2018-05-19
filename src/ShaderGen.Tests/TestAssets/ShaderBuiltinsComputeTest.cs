using System.Numerics;
using System.Runtime.InteropServices;
using ShaderGen;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    /// <summary>
    /// 
    /// </summary>
    public class ShaderBuiltinsComputeTest
    {

        /// <summary>
        /// Used to pass data to/from Compute Shader
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Parameters
        {
            public Matrix4x4 P1_Matrix;
            public Matrix4x4 P2_Matrix;
            public Vector2 P1_Vector2;
            public float P1_Float;
            public float P2_Float;
            public Vector2 P2_V2;
            public float P3_Float;
            public float P4_Float;
            public Vector3 P1_Vector3;
            public float P5_Float;
            public Vector3 P2_Vector3;
            public float P6_Float;
            public Vector4 P1_Vector4;
            public Vector4 P2_Vector4;
        }

        [ResourceSet(0)] public RWStructuredBuffer<Parameters> InOutParameters;

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
            // TODO
        }
    }
}