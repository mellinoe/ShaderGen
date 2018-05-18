using ShaderGen;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestShaders
{
    public class VectorStaticFunctions
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct VectorHolder
        {
            public Vector4 V4;
            public Vector3 V3;
            private float _padding0;
            public Vector2 V2;
            private float _padding1;
            private float _padding2;
            public Matrix4x4 M4x4;
        }

        public VectorHolder VH;

        [VertexShader]
        SystemPosition4 VS(Position4 input)
        {
            Vector2 r2 = Vector2.Abs(VH.V2);
            r2 = Vector2.Add(VH.V2, VH.V2);
            r2 = Vector2.Clamp(VH.V2, VH.V2, VH.V2);
            float r = Vector2.Distance(VH.V2, VH.V2);
            r = Vector2.DistanceSquared(VH.V2, VH.V2);
            r2 = Vector2.Divide(VH.V2, VH.V2);
            r2 = Vector2.Divide(VH.V2, r);
            r = Vector2.Dot(VH.V2, VH.V2);
            r2 = Vector2.Lerp(VH.V2, VH.V2, 0.75f);
            r2 = Vector2.Max(VH.V2, VH.V2);
            r2 = Vector2.Min(VH.V2, VH.V2);
            r2 = Vector2.Multiply(VH.V2, VH.V2);
            r2 = Vector2.Multiply(VH.V2, r);
            r2 = Vector2.Multiply(r, VH.V2);
            r2 = Vector2.Negate(VH.V2);
            r2 = Vector2.Normalize(VH.V2);
            r2 = Vector2.Reflect(VH.V2, VH.V2);
            r2 = Vector2.SquareRoot(VH.V2);
            r2 = Vector2.Subtract(VH.V2, VH.V2);
            r = VH.V2.Length();
            r = VH.V2.LengthSquared();
            r2 = Vector2.Transform(VH.V2, VH.M4x4);

            Vector3 V3 = new Vector3(1, 2, 3);
            Vector3 r3 = Vector3.Abs(VH.V3);
            r3 = Vector3.Add(VH.V3, VH.V3);
            r3 = Vector3.Clamp(VH.V3, VH.V3, VH.V3);
            r3 = Vector3.Cross(VH.V3, VH.V3);
            r = Vector3.Distance(VH.V3, VH.V3);
            r = Vector3.DistanceSquared(VH.V3, VH.V3);
            r3 = Vector3.Divide(VH.V3, VH.V3);
            r3 = Vector3.Divide(VH.V3, r);
            r = Vector3.Dot(VH.V3, VH.V3);
            r3 = Vector3.Lerp(VH.V3, VH.V3, 0.75f);
            r3 = Vector3.Max(VH.V3, VH.V3);
            r3 = Vector3.Min(VH.V3, VH.V3);
            r3 = Vector3.Multiply(VH.V3, VH.V3);
            r3 = Vector3.Multiply(VH.V3, r);
            r3 = Vector3.Multiply(r, VH.V3);
            r3 = Vector3.Negate(VH.V3);
            r3 = Vector3.Normalize(VH.V3);
            r3 = Vector3.Reflect(VH.V3, VH.V3);
            r3 = Vector3.SquareRoot(VH.V3);
            r3 = Vector3.Subtract(VH.V3, VH.V3);
            r = VH.V3.Length();
            r = VH.V3.LengthSquared();
            r3 = Vector3.Transform(VH.V3, VH.M4x4);

            Vector4 V4 = new Vector4(1, 2, 3, 4);
            Vector4 r4 = Vector4.Abs(VH.V4);
            r4 = Vector4.Add(VH.V4, VH.V4);
            r4 = Vector4.Clamp(VH.V4, VH.V4, VH.V4);
            r = Vector4.Distance(VH.V4, VH.V4);
            r = Vector4.DistanceSquared(VH.V4, VH.V4);
            r4 = Vector4.Divide(VH.V4, VH.V4);
            r4 = Vector4.Divide(VH.V4, r);
            r = Vector4.Dot(VH.V4, VH.V4);
            r4 = Vector4.Lerp(VH.V4, VH.V4, 0.75f);
            r4 = Vector4.Max(VH.V4, VH.V4);
            r4 = Vector4.Min(VH.V4, VH.V4);
            r4 = Vector4.Multiply(VH.V4, VH.V4);
            r4 = Vector4.Multiply(VH.V4, r);
            r4 = Vector4.Multiply(r, VH.V4);
            r4 = Vector4.Negate(VH.V4);
            r4 = Vector4.Normalize(VH.V4);
            r4 = Vector4.SquareRoot(VH.V4);
            r4 = Vector4.Subtract(VH.V4, VH.V4);
            r = VH.V4.Length();
            r = VH.V4.LengthSquared();
            r4 = Vector4.Transform(VH.V2, VH.M4x4);
            r4 = Vector4.Transform(VH.V3, VH.M4x4);
            r4 = Vector4.Transform(VH.V4, VH.M4x4);

            SystemPosition4 output;
            output.Position = input.Position;
            return output;
        }
    }
}
