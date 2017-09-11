using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace ShaderGen.Tests.TestAssets
{
    public class TextureSamplerFragment
    {
        public struct FragmentInput
        {
            [VertexSemantic(SemanticType.Position)]
            public Vector4 Position;
            [VertexSemantic(SemanticType.TextureCoordinate)]
            public Vector2 TextureCoordinate;
        }

        [Resource(0)]
        public Texture2DResource Texture;

        [Resource(1)]
        public SamplerResource Sampler;

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            return Sample(Texture, Sampler, input.TextureCoordinate);
        }
    }
}
