using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    public class StructuredBufferTestShader
    {
        public StructuredBuffer<Matrix4x4> StructuredInput;
        public StructuredBuffer<TestStructure> TestStructures;

        [VertexShader]
        public VertexOutput VS(PositionTexture input)
        {
            Matrix4x4 World = StructuredInput[0];
            Matrix4x4 View = StructuredInput[1];
            Matrix4x4 Projection = StructuredInput[2];

            VertexOutput output;
            Vector4 worldPosition = Mul(World, new Vector4(input.Position, 1));
            Vector4 viewPosition = Mul(View, worldPosition);
            output.Position = Mul(Projection, viewPosition);
            output.TextureCoord = input.TextureCoord;
            return output;
        }

        public struct VertexOutput
        {
            [VertexSemantic(SemanticType.SystemPosition)]
            public Vector4 Position;
            [VertexSemantic(SemanticType.TextureCoordinate)]
            public Vector2 TextureCoord;
        }

        public struct TestStructure
        {
            public Vector4 X;
            public Vector2 Y;
            public Vector2 Z;
            public Vector4 W;
        }
    }

}
