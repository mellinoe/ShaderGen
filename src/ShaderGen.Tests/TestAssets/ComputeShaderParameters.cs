using System.Numerics;
using System.Runtime.InteropServices;

namespace TestShaders
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ComputeShaderParameters
    {
        public Matrix4x4 P1Matrix;
        public Matrix4x4 P2Matrix;

        public Vector4 P1Vector4;
        public Vector4 P2Vector4;
        public Vector4 P3Vector4;
        public Vector4 OutVector4;

        public Vector3 P1Vector3;
        public float P1Float;
        public Vector3 P2Vector3;
        public float P2Float;
        public Vector3 P3Vector3;
        public float P3Float;
        public Vector3 OutVector3;
        public float OutFloat;

        public Vector2 P1Vector2;
        public Vector2 P2Vector2;
        public Vector2 P3Vector2;
        public Vector2 OutVector2;
    }
}