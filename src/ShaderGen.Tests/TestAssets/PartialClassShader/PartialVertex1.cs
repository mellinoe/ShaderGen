using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public partial class PartialVertex
    {
        struct FragmentInput
        {
            [VertexSemantic(SemanticType.SystemPosition)] public Vector4 Position;
            [VertexSemantic(SemanticType.Color)] public Vector4 Color;
        }

        public SamplerResource Sampler;

        [VertexShader]
        FragmentInput VertexShaderFunc(VertexInput input)
        {
            FragmentInput output;
            output.Position = new Vector4(input.Position, 1);
            output.Color = input.Color;
            return output;
        }
    }
}
