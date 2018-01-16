using System.Runtime.InteropServices;
using ShaderGen;

namespace TestShaders
{
    internal class UIntVectors
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct VertexInput
        {
            [PositionSemantic]
            public UInt2 U32x2;
            [TextureCoordinateSemantic]
            public UInt3 U32x3;
            [ColorSemantic]
            public UInt4 U32x4;

            [PositionSemantic]
            public Int2 I32x2;
            [PositionSemantic]
            public Int3 I32x3;
            [PositionSemantic]
            public Int4 I32x4;
        }

        [VertexShader]
        public SystemPosition4 VS(VertexInput input)
        {
            SystemPosition4 output;
            output.Position = new System.Numerics.Vector4(
                input.U32x2.X + input.U32x3.X + input.U32x4.X,
                input.U32x2.Y + input.U32x3.Y + input.U32x4.Y,
                input.U32x3.Z + input.U32x4.Z,
                input.U32x4.Z);

            output.Position += new System.Numerics.Vector4(
                input.I32x2.X + input.I32x3.X + input.I32x4.X,
                input.I32x2.Y + input.I32x3.Y + input.I32x4.Y,
                input.I32x3.Z + input.I32x4.Z,
                input.I32x4.W);

            return output;
        }
    }
}
