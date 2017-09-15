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
}
