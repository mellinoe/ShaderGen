using ShaderGen;

namespace TestShaders
{
    internal class UIntVectors
    {
        public struct VertexInput
        {
            [PositionSemantic]
            public UInt2 U2;
            [TextureCoordinateSemantic]
            public UInt3 U3;
            [ColorSemantic]
            public UInt4 U4;
        }

        [VertexShader]
        public SystemPosition4 VS(VertexInput input)
        {
            SystemPosition4 output;
            output.Position = new System.Numerics.Vector4(
                input.U2.X + input.U3.X + input.U4.X,
                input.U2.Y + input.U3.Y + input.U4.Y,
                input.U3.Z + input.U4.Z,
                input.U4.Z);
            return output;
        }
    }
}
