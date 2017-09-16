using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    public class ComplexExpression
    {
        public struct FragmentInput
        {
            [VertexSemantic(SemanticType.Position)]
            public Vector4 Position;
            [VertexSemantic(SemanticType.TextureCoordinate)]
            public Vector2 TextureCoordinate;
        }

        public struct TintInfo
        {
            public Vector3 Color;
            public float Factor;
        }

        public TintInfo Tint;
        public Texture2DResource Texture;
        public SamplerResource Sampler;

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            Vector4 tintValue = new Vector4(Tint.Color, 1);
            Vector4 textureValue = Sample(Texture, Sampler, input.TextureCoordinate);
            return (tintValue * Tint.Factor) + (textureValue * (1 - Tint.Factor));
        }
    }
}
