using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class VectorStaticFunctions
    {
        [VertexShader]
        Position4 VS(Position4 input)
        {
            Vector2 v2 = new Vector2(1, 2);
            Vector2 r2 = Vector2.Abs(v2);
            r2 = Vector2.Add(v2, v2);
            r2 = Vector2.Clamp(v2, v2, v2);
            float r = Vector2.Distance(v2, v2);
            r = Vector2.DistanceSquared(v2, v2);
            r2 = Vector2.Divide(v2, v2);
            r2 = Vector2.Divide(v2, r);
            r = Vector2.Dot(v2, v2);
            r2 = Vector2.Lerp(v2, v2, 0.75f);
            r2 = Vector2.Max(v2, v2);
            r2 = Vector2.Min(v2, v2);
            r2 = Vector2.Multiply(v2, v2);
            r2 = Vector2.Multiply(v2, r);
            r2 = Vector2.Multiply(r, v2);
            r2 = Vector2.Negate(v2);
            r2 = Vector2.Normalize(v2);
            r2 = Vector2.Reflect(v2, v2);
            r2 = Vector2.SquareRoot(v2);
            r2 = Vector2.Subtract(v2, v2);
            r = v2.Length();
            r = v2.LengthSquared();

            Vector3 v3 = new Vector3(1, 2, 3);
            Vector3 r3 = Vector3.Abs(v3);
            r3 = Vector3.Add(v3, v3);
            r3 = Vector3.Clamp(v3, v3, v3);
            r3 = Vector3.Cross(v3, v3);
            r = Vector3.Distance(v3, v3);
            r = Vector3.DistanceSquared(v3, v3);
            r3 = Vector3.Divide(v3, v3);
            r3 = Vector3.Divide(v3, r);
            r = Vector3.Dot(v3, v3);
            r3 = Vector3.Lerp(v3, v3, 0.75f);
            r3 = Vector3.Max(v3, v3);
            r3 = Vector3.Min(v3, v3);
            r3 = Vector3.Multiply(v3, v3);
            r3 = Vector3.Multiply(v3, r);
            r3 = Vector3.Multiply(r, v3);
            r3 = Vector3.Negate(v3);
            r3 = Vector3.Normalize(v3);
            r3 = Vector3.Reflect(v3, v3);
            r3 = Vector3.SquareRoot(v3);
            r3 = Vector3.Subtract(v3, v3);
            r = v3.Length();
            r = v3.LengthSquared();

            Vector4 v4 = new Vector4(1, 2, 3, 4);
            Vector4 r4 = Vector4.Abs(v4);
            r4 = Vector4.Add(v4, v4);
            r4 = Vector4.Clamp(v4, v4, v4);
            r = Vector4.Distance(v4, v4);
            r = Vector4.DistanceSquared(v4, v4);
            r4 = Vector4.Divide(v4, v4);
            r4 = Vector4.Divide(v4, r);
            r = Vector4.Dot(v4, v4);
            r4 = Vector4.Lerp(v4, v4, 0.75f);
            r4 = Vector4.Max(v4, v4);
            r4 = Vector4.Min(v4, v4);
            r4 = Vector4.Multiply(v4, v4);
            r4 = Vector4.Multiply(v4, r);
            r4 = Vector4.Multiply(r, v4);
            r4 = Vector4.Negate(v4);
            r4 = Vector4.Normalize(v4);
            r4 = Vector4.SquareRoot(v4);
            r4 = Vector4.Subtract(v4, v4);
            r = v4.Length();
            r = v4.LengthSquared();

            return input;
        }
    }
}
