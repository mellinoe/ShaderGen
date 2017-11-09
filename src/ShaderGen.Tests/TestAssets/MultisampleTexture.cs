using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    public class MultisampleTexture
    {
        public Texture2DMSResource MultisampleTex2D;
        public SamplerResource Sampler;

        public struct PositionTexture
        {
            [PositionSemantic] public Vector4 Position;
            [TextureCoordinateSemantic] public Vector2 TexCoords;
        }

        public struct FragmentInput
        {
            [SystemPositionSemantic] public Vector4 Position;
        }

        [VertexShader]
        public FragmentInput VS(PositionTexture input)
        {
            FragmentInput output;
            output.Position = Load(MultisampleTex2D, Sampler, input.TexCoords, 0);
            return output;
        }
    }
}
