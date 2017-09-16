using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public struct PositionTexture
    {
        [PositionSemantic]
        public Vector3 Position;
        [TextureCoordinateSemantic]
        public Vector2 TextureCoord;
    }

    public struct Position4Texture2
    {
        [PositionSemantic] public Vector4 Position;
        [TextureCoordinateSemantic] public Vector2 TextureCoord;
    }
}
