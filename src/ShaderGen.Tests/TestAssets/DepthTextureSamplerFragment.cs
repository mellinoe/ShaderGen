using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    public class DepthTextureSamplerFragment
    {
        public struct FragmentInput
        {
            [VertexSemantic(SemanticType.SystemPosition)]
            public Vector4 Position;
            [VertexSemantic(SemanticType.TextureCoordinate)]
            public Vector2 TextureCoordinate;
        }

        public Texture2DResource Tex2D;
        public Texture2DArrayResource TexArray;
        public SamplerComparisonResource Sampler;

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            float arraySample = SampleComparisonLevelZero(TexArray, Sampler, new Vector2(1, 2), 3, 0.5f);
            return new Vector4(
                SampleComparisonLevelZero(Tex2D, Sampler, input.TextureCoordinate, 0.5f),
                SampleMethod(Tex2D, Sampler), 
                0, 
                1);
        }

        private float SampleMethod(Texture2DResource depthTexture, SamplerComparisonResource mySampler)
        {
            return SampleComparisonLevelZero(depthTexture, mySampler, Vector2.Zero, 0.2f)
                + SampleMethodInner(depthTexture, mySampler);
        }

        private float SampleMethodInner(Texture2DResource depthTexture2, SamplerComparisonResource mySampler)
        {
            return SampleComparisonLevelZero(depthTexture2, mySampler, Vector2.Zero, 0.2f);
        }
    }
}
