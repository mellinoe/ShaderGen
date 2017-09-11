using System;
using System.Numerics;

namespace ShaderGen
{
    public static class ShaderBuiltins
    {
        public static Vector4 Mul(Matrix4x4 m, Vector4 v) => throw new ShaderBuiltinException();
        public static Vector4 Sample(Texture2DResource texture, SamplerResource sampler, Vector2 coordinate) => throw new ShaderBuiltinException();
    }

    internal class ShaderBuiltinException : Exception { }
}
