using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    public class ShaderBuiltinsTestShader
    {
        [VertexShader]
        public SystemPosition4 VS(Position4 input)
        {
            float f = 0;
            Vector2 v2 = new Vector2(0, 0);
            Vector3 v3 = new Vector3(0, 0, 0);
            Vector4 v4 = new Vector4(0, 0, 0, 0);
            float r = 0;
            Vector2 r2 = new Vector2(0, 0);
            Vector3 r3 = new Vector3(0, 0, 0);
            Vector4 r4 = new Vector4(0, 0, 0, 0);

            // Abs
            r = Abs(f);
            r2 = Abs(v2);
            r3 = Abs(v3);
            r4 = Abs(v4);

            // Acos
            r = Acos(f);
            r2 = Acos(v2);
            r3 = Acos(v3);
            r4 = Acos(v4);

            // Clamp
            r = Clamp(f, 0, 10);
            r2 = Clamp(v2, new Vector2(0, 0), new Vector2(10, 10));
            r3 = Clamp(v3, new Vector3(0, 0, 0), new Vector3(10, 10, 10));
            r4 = Clamp(v4, new Vector4(0, 0, 0, 0), new Vector4(10, 10, 10, 10));

            // Pow
            r = Pow(f, 10);
            r2 = Pow(v2, new Vector2(10, 12));
            r3 = Pow(v3, new Vector3(10, 12, 14));
            r4 = Pow(v4, new Vector4(10, 12, 14, 16));

            // Saturate
            r = Saturate(f);
            r2 = Saturate(v2);
            r3 = Saturate(v3);
            r4 = Saturate(v4);

            // Tan
            r = Tan(f);
            r2 = Tan(v2);
            r3 = Tan(v3);
            r4 = Tan(v4);

            // Mod
            r = Mod(f, 2);
            r2 = Mod(v2, new Vector2(2, 4));
            r3 = Mod(v3, new Vector3(2, 4, 6));
            r4 = Mod(v4, new Vector4(2, 4, 6, 8));

            // ClipToTextureCoordinates
            r2 = ClipToTextureCoordinates(v4);

            SystemPosition4 output;
            output.Position = input.Position;
            return output;
        }
    }
}
