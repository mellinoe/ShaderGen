using ShaderGen;
using System.Numerics;
using System.Runtime.CompilerServices;
namespace TestShaders.VeldridShaders
{
    internal class UIntVertexAttribs
    {
        public struct Vertex
        {
            [PositionSemantic]
            public Vector2 Position;
            [ColorSemantic]
            public UInt4 Color_Int;
        }

        public struct FragmentInput
        {
            [SystemPositionSemantic]
            public Vector4 Position;
            [ColorSemantic]
            public Vector4 Color;
        }

        public struct Info
        {
            public uint ColorNormalizationFactor;
            public Vector3 Padding0__;
        }

        public Info InfoBuffer;

        [VertexShader]
        public FragmentInput VS(Vertex input)
        {
            FragmentInput output;
            output.Position = new Vector4(input.Position, 0, 1);
            output.Color = new Vector4(input.Color_Int.X, input.Color_Int.Y, input.Color_Int.Z, input.Color_Int.W) / InfoBuffer.ColorNormalizationFactor;
            return output;
        }

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            return input.Color;
        }
    }
}
