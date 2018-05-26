using System.Numerics;
using ShaderGen;
using static ShaderGen.Builtins;

namespace TestShaders.VeldridShaders
{
    public class ShadowDepth
    {
        public Matrix4x4 Projection;
        public Matrix4x4 View;
        public Matrix4x4 World;

        [VertexShader]
        public FragmentInput VS(VertexInput input)
        {
            FragmentInput output;
            output.Position = Mul(Projection, Mul(View, Mul(World, new Vector4(input.Position, 1))));
            return output;
        }

        [FragmentShader]
        public void FS(FragmentInput input) { }

        public struct VertexInput
        {
            [PositionSemantic] public Vector3 Position;
            [NormalSemantic] public Vector3 Normal;
            [TextureCoordinateSemantic] public Vector2 TexCoord;
        }

        public struct FragmentInput
        {
            [SystemPositionSemantic]
            public Vector4 Position;
        }
    }
}
