using System;
using System.Numerics;

namespace ShaderGen
{
    public static class ShaderBuiltins
    {
        public static Vector4 Mul(Matrix4x4 m, Vector4 v) => throw new ShaderBuiltinException();
    }

    internal class ShaderBuiltinException : Exception { }
}
