using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class VectorStaticProperties
    {
        [VertexShader]
        Position4 VS(Position4 input)
        {
            Vector2 v2 = Vector2.Zero;
            v2 = Vector2.One;
            v2 = Vector2.UnitX;
            v2 = Vector2.UnitY;

            Vector3 v3 = Vector3.Zero;
            v3 = Vector3.One;
            v3 = Vector3.UnitX;
            v3 = Vector3.UnitY;
            v3 = Vector3.UnitZ;

            Vector4 v4 = Vector4.Zero;
            v4 = Vector4.One;
            v4 = Vector4.UnitX;
            v4 = Vector4.UnitY;
            v4 = Vector4.UnitZ;
            v4 = Vector4.UnitW;

            return input;
        }
    }
}
