using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public struct PositionTexture
    {
        [VertexSemantic(SemanticType.Position)]
        public Vector3 Position;
        [VertexSemantic(SemanticType.TextureCoordinate)]
        public Vector2 TextureCoord;
    }
}
