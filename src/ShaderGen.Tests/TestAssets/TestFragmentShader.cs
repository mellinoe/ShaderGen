using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    public class TestFragmentShader
    {
        [FragmentShader]
        public Vector4 FS(VertexOutput input)
        {
            return input.Color;
        }

        public struct VertexOutput
        {
            [VertexSemantic(SemanticType.SystemPosition)]
            public Vector4 Position;
            [VertexSemantic(SemanticType.Color)]
            public Vector4 Color;
        }
    }
}
