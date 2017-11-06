using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public struct Position4
    {
        [PositionSemantic] public Vector4 Position;
    }

    public struct SystemPosition4
    {
        [SystemPositionSemantic] public Vector4 Position;
    }
}
