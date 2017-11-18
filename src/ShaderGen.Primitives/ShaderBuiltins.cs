using System;
using System.Numerics;

namespace ShaderGen
{
    public static class ShaderBuiltins
    {
        // Misc
        public static Vector4 Mul(Matrix4x4 m, Vector4 v) => throw new ShaderBuiltinException();
        public static Vector4 Sample(Texture2DResource texture, SamplerResource sampler, Vector2 texCoords)
            => throw new ShaderBuiltinException();
        public static Vector4 Sample(TextureCubeResource texture, SamplerResource sampler, Vector3 texCoords)
            => throw new ShaderBuiltinException();
        public static Vector4 Load(
            Texture2DMSResource texture,
            SamplerResource sampler,
            Vector2 texCoords,
            uint sampleIndex) => throw new ShaderBuiltinException();
        public static void Discard() => throw new ShaderBuiltinException();
        public static Vector2 ClipToTextureCoordinates(Vector4 clipCoordinates) => throw new ShaderBuiltinException();

        // Abs
        public static float Abs(float value) => throw new ShaderBuiltinException();
        public static Vector2 Abs(Vector2 value) => throw new ShaderBuiltinException();
        public static Vector3 Abs(Vector3 value) => throw new ShaderBuiltinException();
        public static Vector4 Abs(Vector4 value) => throw new ShaderBuiltinException();

        // Acos
        public static float Acos(float value) => throw new ShaderBuiltinException();
        public static Vector2 Acos(Vector2 value) => throw new ShaderBuiltinException();
        public static Vector3 Acos(Vector3 value) => throw new ShaderBuiltinException();
        public static Vector4 Acos(Vector4 value) => throw new ShaderBuiltinException();


        // Clamp
        public static float Clamp(float value, float min, float max) => throw new ShaderBuiltinException();
        public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max) => throw new ShaderBuiltinException();
        public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max) => throw new ShaderBuiltinException();
        public static Vector4 Clamp(Vector4 value, Vector4 min, Vector4 max) => throw new ShaderBuiltinException();

        // Mod
        public static float Mod(float a, float b) => throw new ShaderBuiltinException();
        public static Vector2 Mod(Vector2 a, Vector2 b) => throw new ShaderBuiltinException();
        public static Vector3 Mod(Vector3 a, Vector3 b) => throw new ShaderBuiltinException();
        public static Vector4 Mod(Vector4 a, Vector4 b) => throw new ShaderBuiltinException();

        // Pow
        public static float Pow(float x, float y) => throw new ShaderBuiltinException();
        public static Vector2 Pow(Vector2 x, Vector2 y) => throw new ShaderBuiltinException();
        public static Vector3 Pow(Vector3 x, Vector3 y) => throw new ShaderBuiltinException();
        public static Vector4 Pow(Vector4 x, Vector4 y) => throw new ShaderBuiltinException();

        // Saturate
        public static float Saturate(float value) => throw new ShaderBuiltinException();
        public static Vector2 Saturate(Vector2 value) => throw new ShaderBuiltinException();
        public static Vector3 Saturate(Vector3 value) => throw new ShaderBuiltinException();
        public static Vector4 Saturate(Vector4 value) => throw new ShaderBuiltinException();

        // Tan
        public static float Tan(float value) => throw new ShaderBuiltinException();
        public static Vector2 Tan(Vector2 value) => throw new ShaderBuiltinException();
        public static Vector3 Tan(Vector3 value) => throw new ShaderBuiltinException();
        public static Vector4 Tan(Vector4 value) => throw new ShaderBuiltinException();

        // Built-in variables
        public static uint VertexID => throw new ShaderBuiltinException();
        public static uint InstanceID => throw new ShaderBuiltinException();
        public static UInt3 DispatchThreadID => throw new ShaderBuiltinException();
        public static UInt3 GroupThreadID => throw new ShaderBuiltinException();
    }

    internal class ShaderBuiltinException : Exception { }
}
