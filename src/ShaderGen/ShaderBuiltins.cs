using System;
using System.Numerics;

namespace ShaderGen
{
    public static class ShaderBuiltins
    {
        public static Vector4 Mul(Matrix4x4 m, Vector4 v) => throw new ShaderBuiltinException();
        public static Vector4 Sample(Texture2DResource texture, SamplerResource sampler, Vector2 texCoords) => throw new ShaderBuiltinException();
        public static Vector4 Sample(TextureCubeResource texture, SamplerResource sampler, Vector3 texCoords) => throw new ShaderBuiltinException();
        public static float Saturate(float value) => throw new ShaderBuiltinException();
        public static float Pow(float x, float y) => throw new ShaderBuiltinException();
        public static float Clamp(float value, float min, float max) => throw new ShaderBuiltinException();
        public static float Abs(float value) => throw new ShaderBuiltinException();
        public static float Acos(float value) => throw new ShaderBuiltinException();
        public static float Tan(float value) => throw new ShaderBuiltinException();
        public static Vector4 Saturate(Vector4 value) => throw new ShaderBuiltinException();
        public static void Discard() => throw new ShaderBuiltinException();
    }

    internal class ShaderBuiltinException : Exception { }
}
