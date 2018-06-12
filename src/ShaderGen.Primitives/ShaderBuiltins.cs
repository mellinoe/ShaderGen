using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ShaderGen
{
    public static class ShaderBuiltins
    {
        /*
         * Constants
         */
        public const float PI = (float)Math.PI;

        public const float E = (float)Math.E;

        public const float DegreesPerRadian = 57.2957795130823f;

        /*
         * Misc
         */
        public static Vector4 Sample(Texture2DResource texture, SamplerResource sampler, Vector2 texCoords)
            => throw new ShaderBuiltinException();
        public static Vector4 Sample(TextureCubeResource texture, SamplerResource sampler, Vector3 texCoords)
            => throw new ShaderBuiltinException();
        public static Vector4 Sample(Texture2DArrayResource texture, SamplerResource sampler, Vector2 texCoords, uint arrayLayer)
            => throw new ShaderBuiltinException();
        public static Vector4 SampleGrad(Texture2DResource texture, SamplerResource sampler, Vector2 texCoords, Vector2 ddx, Vector2 ddy)
            => throw new ShaderBuiltinException();
        public static Vector4 SampleGrad(Texture2DArrayResource texture, SamplerResource sampler, Vector2 texCoords, uint arrayLayer, Vector2 ddx, Vector2 ddy)
            => throw new ShaderBuiltinException();
        public static Vector4 Load(
            Texture2DResource texture,
            SamplerResource sampler,
            Vector2 texCoords,
            uint lod) => throw new ShaderBuiltinException();
        public static Vector4 Load(
            Texture2DMSResource texture,
            SamplerResource sampler,
            Vector2 texCoords,
            uint sampleIndex) => throw new ShaderBuiltinException();
        public static T Load<T>(RWTexture2DResource<T> texture, UInt2 texCoords) where T : struct => throw new ShaderBuiltinException();
        public static T Store<T>(RWTexture2DResource<T> texture, UInt2 texCoords, T value) where T : struct => throw new ShaderBuiltinException();
        public static float SampleComparisonLevelZero(DepthTexture2DResource texture, SamplerComparisonResource sampler, Vector2 texCoords, float compareValue)
            => throw new ShaderBuiltinException();
        public static float SampleComparisonLevelZero(DepthTexture2DArrayResource texture, SamplerComparisonResource sampler, Vector2 texCoords, uint arrayLayer, float compareValue)
            => throw new ShaderBuiltinException();
        public static void Discard() => throw new ShaderBuiltinException();
        public static Vector2 ClipToTextureCoordinates(Vector4 clipCoordinates) => throw new ShaderBuiltinException();

        // Ddx
        public static float Ddx(float value) => throw new ShaderBuiltinException();
        public static Vector2 Ddx(Vector2 value) => throw new ShaderBuiltinException();
        public static Vector3 Ddx(Vector3 value) => throw new ShaderBuiltinException();
        public static Vector4 Ddx(Vector4 value) => throw new ShaderBuiltinException();

        // DdxFine
        public static float DdxFine(float value) => throw new ShaderBuiltinException();
        public static Vector2 DdxFine(Vector2 value) => throw new ShaderBuiltinException();
        public static Vector3 DdxFine(Vector3 value) => throw new ShaderBuiltinException();
        public static Vector4 DdxFine(Vector4 value) => throw new ShaderBuiltinException();

        // Ddy
        public static float Ddy(float value) => throw new ShaderBuiltinException();
        public static Vector2 Ddy(Vector2 value) => throw new ShaderBuiltinException();
        public static Vector3 Ddy(Vector3 value) => throw new ShaderBuiltinException();
        public static Vector4 Ddy(Vector4 value) => throw new ShaderBuiltinException();

        // DdyFine
        public static float DdyFine(float value) => throw new ShaderBuiltinException();
        public static Vector2 DdyFine(Vector2 value) => throw new ShaderBuiltinException();
        public static Vector3 DdyFine(Vector3 value) => throw new ShaderBuiltinException();
        public static Vector4 DdyFine(Vector4 value) => throw new ShaderBuiltinException();

        // Interlocked
        public static uint InterlockedAdd(AtomicBufferUInt32 buffer, uint index, uint value) => throw new ShaderBuiltinException();
        public static uint InterlockedAdd(AtomicBufferUInt32 buffer, int index, uint value) => throw new ShaderBuiltinException();
        public static int InterlockedAdd(AtomicBufferInt32 buffer, uint index, int value) => throw new ShaderBuiltinException();
        public static int InterlockedAdd(AtomicBufferInt32 buffer, int index, int value) => throw new ShaderBuiltinException();

        // Built-in variables
        public static uint VertexID => throw new ShaderBuiltinException();
        public static uint InstanceID => throw new ShaderBuiltinException();
        public static UInt3 DispatchThreadID => throw new ShaderBuiltinException();
        public static UInt3 GroupThreadID => throw new ShaderBuiltinException();
        public static bool IsFrontFace => throw new ShaderBuiltinException();


        /*
         * CPU Compatible
         * TODO Consider placing in seperate class to make clear which functions can be used on CPU
         */

        // Abs
        public static float Abs(float value) => Math.Abs(value);
        public static Vector2 Abs(Vector2 value) => new Vector2(Math.Abs(value.X), Math.Abs(value.Y));
        public static Vector3 Abs(Vector3 value) => new Vector3(Math.Abs(value.X), Math.Abs(value.Y), Math.Abs(value.Z));
        public static Vector4 Abs(Vector4 value) => new Vector4(Math.Abs(value.X), Math.Abs(value.Y), Math.Abs(value.Z), Math.Abs(value.W));

        // Acos
        public static float Acos(float value) => (float)Math.Acos(value);
        public static Vector2 Acos(Vector2 value) => new Vector2((float)Math.Acos(value.X), (float)Math.Acos(value.Y));
        public static Vector3 Acos(Vector3 value) => new Vector3((float)Math.Acos(value.X), (float)Math.Acos(value.Y), (float)Math.Acos(value.Z));
        public static Vector4 Acos(Vector4 value) => new Vector4((float)Math.Acos(value.X), (float)Math.Acos(value.Y), (float)Math.Acos(value.Z), (float)Math.Acos(value.W));

        // Acosh
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Acosh(float value) => (float)Math.Log(value + Math.Sqrt(value * value - 1.0));
        public static Vector2 Acosh(Vector2 value) => new Vector2(Acosh(value.X), Acosh(value.Y));
        public static Vector3 Acosh(Vector3 value) => new Vector3(Acosh(value.X), Acosh(value.Y), Acosh(value.Z));
        public static Vector4 Acosh(Vector4 value) => new Vector4(Acosh(value.X), Acosh(value.Y), Acosh(value.Z), Acosh(value.W));

        // Asin
        public static float Asin(float value) => (float)Math.Asin(value);
        public static Vector2 Asin(Vector2 value) => new Vector2((float)Math.Asin(value.X), (float)Math.Asin(value.Y));
        public static Vector3 Asin(Vector3 value) => new Vector3((float)Math.Asin(value.X), (float)Math.Asin(value.Y), (float)Math.Asin(value.Z));
        public static Vector4 Asin(Vector4 value) => new Vector4((float)Math.Asin(value.X), (float)Math.Asin(value.Y), (float)Math.Asin(value.Z), (float)Math.Asin(value.W));

        // Asinh
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Asinh(float value) => (float)Math.Log(value + Math.Sqrt(value * value + 1.0));
        public static Vector2 Asinh(Vector2 value) => new Vector2(Asinh(value.X), Asinh(value.Y));
        public static Vector3 Asinh(Vector3 value) => new Vector3(Asinh(value.X), Asinh(value.Y), Asinh(value.Z));
        public static Vector4 Asinh(Vector4 value) => new Vector4(Asinh(value.X), Asinh(value.Y), Asinh(value.Z), Asinh(value.W));

        // Atan (note supports both Atan and Atan2 equivalents as this is how OpenGL works and feels more discoverable
        public static float Atan(float value) => (float)Math.Atan(value);
        public static float Atan(float y, float x) => (float)Math.Atan2(y, x);
        public static Vector2 Atan(Vector2 value) => new Vector2((float)Math.Atan(value.X), (float)Math.Atan(value.Y));
        public static Vector2 Atan(Vector2 y, Vector2 x) => new Vector2((float)Math.Atan2(y.X, x.X), (float)Math.Atan2(y.Y, x.Y));
        public static Vector3 Atan(Vector3 value) => new Vector3((float)Math.Atan(value.X), (float)Math.Atan(value.Y), (float)Math.Atan(value.Z));
        public static Vector3 Atan(Vector3 y, Vector3 x) => new Vector3((float)Math.Atan2(y.X, x.X), (float)Math.Atan2(y.Y, x.Y), (float)Math.Atan2(y.Z, x.Z));
        public static Vector4 Atan(Vector4 value) => new Vector4((float)Math.Atan(value.X), (float)Math.Atan(value.Y), (float)Math.Atan(value.Z), (float)Math.Atan(value.W));
        public static Vector4 Atan(Vector4 y, Vector4 x) => new Vector4((float)Math.Atan2(y.X, x.X), (float)Math.Atan2(y.Y, x.Y), (float)Math.Atan2(y.Z, x.Z), (float)Math.Atan2(y.W, x.W));

        // Atanh
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Atanh(float value) => (float)(Math.Log((1.0f + value) / (1.0f - value)) / 2.0f);
        public static Vector2 Atanh(Vector2 value) => new Vector2(Atanh(value.X), Atanh(value.Y));
        public static Vector3 Atanh(Vector3 value) => new Vector3(Atanh(value.X), Atanh(value.Y), Atanh(value.Z));
        public static Vector4 Atanh(Vector4 value) => new Vector4(Atanh(value.X), Atanh(value.Y), Atanh(value.Z), Atanh(value.W));

        // Cbrt TODO add Matrix support
        private const double _third = 1.0 / 3.0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cbrt(float value) => (float)Math.Pow(Math.Abs(value), _third);
        public static Vector2 Cbrt(Vector2 value) => new Vector2(Cbrt(value.X), Cbrt(value.Y));
        public static Vector3 Cbrt(Vector3 value) => new Vector3(Cbrt(value.X), Cbrt(value.Y), Cbrt(value.Z));
        public static Vector4 Cbrt(Vector4 value) => new Vector4(Cbrt(value.X), Cbrt(value.Y), Cbrt(value.Z), Cbrt(value.W));

        // Ceiling
        public static float Ceiling(float value) => (float)Math.Ceiling(value);
        public static Vector2 Ceiling(Vector2 value) => new Vector2((float)Math.Ceiling(value.X), (float)Math.Ceiling(value.Y));
        public static Vector3 Ceiling(Vector3 value) => new Vector3((float)Math.Ceiling(value.X), (float)Math.Ceiling(value.Y), (float)Math.Ceiling(value.Z));
        public static Vector4 Ceiling(Vector4 value) => new Vector4((float)Math.Ceiling(value.X), (float)Math.Ceiling(value.Y), (float)Math.Ceiling(value.Z), (float)Math.Ceiling(value.W));

        // Clamp TODO add int & uint versions (see https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/clamp.xhtml)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max) => Math.Min(Math.Max(value, min), max);
        public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max) => new Vector2(Clamp(value.X, min.X, max.X), Clamp(value.Y, min.Y, max.Y));
        public static Vector2 Clamp(Vector2 value, float min, float max) => new Vector2(Clamp(value.X, min, max), Clamp(value.Y, min, max));
        public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max) => new Vector3(Clamp(value.X, min.X, max.X), Clamp(value.Y, min.Y, max.Y), Clamp(value.Z, min.Z, max.Z));
        public static Vector3 Clamp(Vector3 value, float min, float max) => new Vector3(Clamp(value.X, min, max), Clamp(value.Y, min, max), Clamp(value.Z, min, max));
        public static Vector4 Clamp(Vector4 value, Vector4 min, Vector4 max) => new Vector4(Clamp(value.X, min.X, max.X), Clamp(value.Y, min.Y, max.Y), Clamp(value.Z, min.Z, max.Z), Clamp(value.W, min.W, max.W));
        public static Vector4 Clamp(Vector4 value, float min, float max) => new Vector4(Clamp(value.X, min, max), Clamp(value.Y, min, max), Clamp(value.Z, min, max), Clamp(value.W, min, max));

        // Cos
        public static float Cos(float value) => (float)Math.Cos(value);
        public static Vector2 Cos(Vector2 value) => new Vector2((float)Math.Cos(value.X), (float)Math.Cos(value.Y));
        public static Vector3 Cos(Vector3 value) => new Vector3((float)Math.Cos(value.X), (float)Math.Cos(value.Y), (float)Math.Cos(value.Z));
        public static Vector4 Cos(Vector4 value) => new Vector4((float)Math.Cos(value.X), (float)Math.Cos(value.Y), (float)Math.Cos(value.Z), (float)Math.Cos(value.W));

        // Cosh
        public static float Cosh(float value) => (float)Math.Cosh(value);
        public static Vector2 Cosh(Vector2 value) => new Vector2((float)Math.Cosh(value.X), (float)Math.Cosh(value.Y));
        public static Vector3 Cosh(Vector3 value) => new Vector3((float)Math.Cosh(value.X), (float)Math.Cosh(value.Y), (float)Math.Cosh(value.Z));
        public static Vector4 Cosh(Vector4 value) => new Vector4((float)Math.Cosh(value.X), (float)Math.Cosh(value.Y), (float)Math.Cosh(value.Z), (float)Math.Cosh(value.W));

        // Degrees
        public static float Degrees(float value) => value * DegreesPerRadian;
        public static Vector2 Degrees(Vector2 value) => new Vector2(value.X * DegreesPerRadian, value.Y * DegreesPerRadian);
        public static Vector3 Degrees(Vector3 value) => new Vector3(value.X * DegreesPerRadian, value.Y * DegreesPerRadian, value.Z * DegreesPerRadian);
        public static Vector4 Degrees(Vector4 value) => new Vector4(value.X * DegreesPerRadian, value.Y * DegreesPerRadian, value.Z * DegreesPerRadian, value.W * DegreesPerRadian);

        // Exp
        public static float Exp(float value) => (float)Math.Exp(value);
        public static Vector2 Exp(Vector2 value) => new Vector2((float)Math.Exp(value.X), (float)Math.Exp(value.Y));
        public static Vector3 Exp(Vector3 value) => new Vector3((float)Math.Exp(value.X), (float)Math.Exp(value.Y), (float)Math.Exp(value.Z));
        public static Vector4 Exp(Vector4 value) => new Vector4((float)Math.Exp(value.X), (float)Math.Exp(value.Y), (float)Math.Exp(value.Z), (float)Math.Exp(value.W));

        // Exp2
        public static float Exp2(float value) => (float) Math.Pow(2.0, value);
        public static Vector2 Exp2(Vector2 value) => new Vector2((float) Math.Pow(2.0, value.X), (float) Math.Pow(2.0, value.Y));
        public static Vector3 Exp2(Vector3 value) => new Vector3((float) Math.Pow(2.0, value.X), (float) Math.Pow(2.0, value.Y), (float) Math.Pow(2.0, value.Z));
        public static Vector4 Exp2(Vector4 value) => new Vector4((float) Math.Pow(2.0, value.X), (float) Math.Pow(2.0, value.Y), (float) Math.Pow(2.0, value.Z), (float) Math.Pow(2.0, value.W));

        // Floor
        public static float Floor(float value) => (float)Math.Floor(value);
        public static Vector2 Floor(Vector2 value) => new Vector2((float)Math.Floor(value.X), (float)Math.Floor(value.Y));
        public static Vector3 Floor(Vector3 value) => new Vector3((float)Math.Floor(value.X), (float)Math.Floor(value.Y), (float)Math.Floor(value.Z));
        public static Vector4 Floor(Vector4 value) => new Vector4((float)Math.Floor(value.X), (float)Math.Floor(value.Y), (float)Math.Floor(value.Z), (float)Math.Floor(value.W));


        // FMod - See https://stackoverflow.com/questions/7610631/glsl-mod-vs-hlsl-fmod
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FMod(float a, float b) => a % b;
        public static Vector2 FMod(Vector2 a, Vector2 b) => new Vector2(FMod(a.X, b.X), FMod(a.Y, b.Y));
        public static Vector2 FMod(Vector2 a, float b) => new Vector2(FMod(a.X, b), FMod(a.Y, b));
        public static Vector3 FMod(Vector3 a, Vector3 b) => new Vector3(FMod(a.X, b.X), FMod(a.Y, b.Y), FMod(a.Z, b.Z));
        public static Vector3 FMod(Vector3 a, float b) => new Vector3(FMod(a.X, b), FMod(a.Y, b), FMod(a.Z, b));
        public static Vector4 FMod(Vector4 a, Vector4 b) => new Vector4(FMod(a.X, b.X), FMod(a.Y, b.Y), FMod(a.Z, b.Z), FMod(a.W, b.W));
        public static Vector4 FMod(Vector4 a, float b) => new Vector4(FMod(a.X, b), FMod(a.Y, b), FMod(a.Z, b), FMod(a.W, b));

        // Frac TODO Check this really is equivalent
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Frac(float value) => (float)(value - Math.Floor(value));
        public static Vector2 Frac(Vector2 value) => new Vector2(Frac(value.X), Frac(value.Y));
        public static Vector3 Frac(Vector3 value) => new Vector3(Frac(value.X), Frac(value.Y), Frac(value.Z));
        public static Vector4 Frac(Vector4 value) => new Vector4(Frac(value.X), Frac(value.Y), Frac(value.Z), Frac(value.W));

        // Lerp
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float x, float y, float s) => x * (1f - s) + y * s;
        public static Vector2 Lerp(Vector2 x, Vector2 y, Vector2 s) => new Vector2(Lerp(x.X, y.X, s.X), Lerp(x.Y, y.Y, s.Y));
        public static Vector2 Lerp(Vector2 x, Vector2 y, float s) => new Vector2(Lerp(x.X, y.X, s), Lerp(x.Y, y.Y, s));
        public static Vector3 Lerp(Vector3 x, Vector3 y, Vector3 s) => new Vector3(Lerp(x.X, y.X, s.X), Lerp(x.Y, y.Y, s.Y), Lerp(x.Z, y.Z, s.Z));
        public static Vector3 Lerp(Vector3 x, Vector3 y, float s) => new Vector3(Lerp(x.X, y.X, s), Lerp(x.Y, y.Y, s), Lerp(x.Z, y.Z, s));
        public static Vector4 Lerp(Vector4 x, Vector4 y, Vector4 s) => new Vector4(Lerp(x.X, y.X, s.X), Lerp(x.Y, y.Y, s.Y), Lerp(x.Z, y.Z, s.Z), Lerp(x.W, y.W, s.W));
        public static Vector4 Lerp(Vector4 x, Vector4 y, float s) => new Vector4(Lerp(x.X, y.X, s), Lerp(x.Y, y.Y, s), Lerp(x.Z, y.Z, s), Lerp(x.W, y.W, s));

        // Log
        public static float Log(float value) => (float)Math.Log(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log(float a, float newBase) => (float)Math.Log(a, newBase);
        public static Vector2 Log(Vector2 value) => new Vector2((float)Math.Log(value.X), (float)Math.Log(value.Y));
        public static Vector2 Log(Vector2 a, Vector2 newBase) => new Vector2(Log(a.X, newBase.X), Log(a.Y, newBase.Y));
        public static Vector2 Log(Vector2 a, float newBase) => new Vector2(Log(a.X, newBase), Log(a.Y, newBase));
        public static Vector3 Log(Vector3 value) => new Vector3((float)Math.Log(value.X), (float)Math.Log(value.Y), (float)Math.Log(value.Z));
        public static Vector3 Log(Vector3 a, Vector3 newBase) => new Vector3(Log(a.X, newBase.X), Log(a.Y, newBase.Y), Log(a.Z, newBase.Z));
        public static Vector3 Log(Vector3 a, float newBase) => new Vector3(Log(a.X, newBase), Log(a.Y, newBase), Log(a.Z, newBase));
        public static Vector4 Log(Vector4 value) => new Vector4((float)Math.Log(value.X), (float)Math.Log(value.Y), (float)Math.Log(value.Z), (float)Math.Log(value.W));
        public static Vector4 Log(Vector4 a, Vector4 newBase) => new Vector4(Log(a.X, newBase.X), Log(a.Y, newBase.Y), Log(a.Z, newBase.Z), Log(a.W, newBase.W));
        public static Vector4 Log(Vector4 a, float newBase) => new Vector4(Log(a.X, newBase), Log(a.Y, newBase), Log(a.Z, newBase), Log(a.W, newBase));

        // Log2
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log2(float value) => Log(value, 2f);
        public static Vector2 Log2(Vector2 value) => new Vector2(Log2(value.X), Log2(value.Y));
        public static Vector3 Log2(Vector3 value) => new Vector3(Log2(value.X), Log2(value.Y), Log2(value.Z));
        public static Vector4 Log2(Vector4 value) => new Vector4(Log2(value.X), Log2(value.Y), Log2(value.Z), Log2(value.W));

        // Log10
        public static float Log10(float value) => (float)Math.Log10(value);
        public static Vector2 Log10(Vector2 value) => new Vector2((float)Math.Log10(value.X), (float)Math.Log10(value.Y));
        public static Vector3 Log10(Vector3 value) => new Vector3((float)Math.Log10(value.X), (float)Math.Log10(value.Y), (float)Math.Log10(value.Z));
        public static Vector4 Log10(Vector4 value) => new Vector4((float)Math.Log10(value.X), (float)Math.Log10(value.Y), (float)Math.Log10(value.Z), (float)Math.Log10(value.W));

        // Max
        public static float Max(float a, float b) => Math.Max(a, b);
        public static Vector2 Max(Vector2 a, Vector2 b) => new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
        public static Vector2 Max(Vector2 a, float b) => new Vector2(Math.Max(a.X, b), Math.Max(a.Y, b));
        public static Vector3 Max(Vector3 a, Vector3 b) => new Vector3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        public static Vector3 Max(Vector3 a, float b) => new Vector3(Math.Max(a.X, b), Math.Max(a.Y, b), Math.Max(a.Z, b));
        public static Vector4 Max(Vector4 a, Vector4 b) => new Vector4(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z), Math.Max(a.W, b.W));
        public static Vector4 Max(Vector4 a, float b) => new Vector4(Math.Max(a.X, b), Math.Max(a.Y, b), Math.Max(a.Z, b), Math.Max(a.W, b));

        // Min
        public static float Min(float a, float b) => Math.Min(a, b);
        public static Vector2 Min(Vector2 a, Vector2 b) => new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
        public static Vector2 Min(Vector2 a, float b) => new Vector2(Math.Min(a.X, b), Math.Min(a.Y, b));
        public static Vector3 Min(Vector3 a, Vector3 b) => new Vector3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        public static Vector3 Min(Vector3 a, float b) => new Vector3(Math.Min(a.X, b), Math.Min(a.Y, b), Math.Min(a.Z, b));
        public static Vector4 Min(Vector4 a, Vector4 b) => new Vector4(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z), Math.Min(a.W, b.W));
        public static Vector4 Min(Vector4 a, float b) => new Vector4(Math.Min(a.X, b), Math.Min(a.Y, b), Math.Min(a.Z, b), Math.Min(a.W, b));

        // Mul
        public static Vector4 Mul(Matrix4x4 m, Vector4 v) => new Vector4(
            m.M11 * v.X + m.M21 * v.Y + m.M31 * v.Z + m.M41 * v.W,
            m.M12 * v.X + m.M22 * v.Y + m.M32 * v.Z + m.M42 * v.W,
            m.M13 * v.X + m.M23 * v.Y + m.M33 * v.Z + m.M43 * v.W,
            m.M14 * v.X + m.M24 * v.Y + m.M34 * v.Z + m.M44 * v.W
        );

        // Mod - See https://stackoverflow.com/questions/7610631/glsl-mod-vs-hlsl-fmod
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Mod(float a, float b) => a - b * (float)Math.Floor(a / b);
        public static Vector2 Mod(Vector2 a, Vector2 b) => new Vector2(Mod(a.X, b.X), Mod(a.Y, b.Y));
        public static Vector2 Mod(Vector2 a, float b) => new Vector2(Mod(a.X, b), Mod(a.Y, b));
        public static Vector3 Mod(Vector3 a, Vector3 b) => new Vector3(Mod(a.X, b.X), Mod(a.Y, b.Y), Mod(a.Z, b.Z));
        public static Vector3 Mod(Vector3 a, float b) => new Vector3(Mod(a.X, b), Mod(a.Y, b), Mod(a.Z, b));
        public static Vector4 Mod(Vector4 a, Vector4 b) => new Vector4(Mod(a.X, b.X), Mod(a.Y, b.Y), Mod(a.Z, b.Z), Mod(a.W, b.W));
        public static Vector4 Mod(Vector4 a, float b) => new Vector4(Mod(a.X, b), Mod(a.Y, b), Mod(a.Z, b), Mod(a.W, b));

        // Pow
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Pow(float x, float y) => (float)Math.Pow(Math.Abs(x), y);
        public static Vector2 Pow(Vector2 y, Vector2 x) => new Vector2(Pow(y.X, x.X), Pow(y.Y, x.Y));
        public static Vector3 Pow(Vector3 y, Vector3 x) => new Vector3(Pow(y.X, x.X), Pow(y.Y, x.Y), Pow(y.Z, x.Z));
        public static Vector4 Pow(Vector4 y, Vector4 x) => new Vector4(Pow(y.X, x.X), Pow(y.Y, x.Y), Pow(y.Z, x.Z), Pow(y.W, x.W));

        // Radians
        public static float Radians(float value) => value / DegreesPerRadian;
        public static Vector2 Radians(Vector2 value) => new Vector2(value.X / DegreesPerRadian, value.Y / DegreesPerRadian);
        public static Vector3 Radians(Vector3 value) => new Vector3(value.X / DegreesPerRadian, value.Y / DegreesPerRadian, value.Z / DegreesPerRadian);
        public static Vector4 Radians(Vector4 value) => new Vector4(value.X / DegreesPerRadian, value.Y / DegreesPerRadian, value.Z / DegreesPerRadian, value.W / DegreesPerRadian);

        // Round
        public static float Round(float value) => (float)Math.Round(value);
        public static Vector2 Round(Vector2 value) => new Vector2((float)Math.Round(value.X), (float)Math.Round(value.Y));
        public static Vector3 Round(Vector3 value) => new Vector3((float)Math.Round(value.X), (float)Math.Round(value.Y), (float)Math.Round(value.Z));
        public static Vector4 Round(Vector4 value) => new Vector4((float)Math.Round(value.X), (float)Math.Round(value.Y), (float)Math.Round(value.Z), (float)Math.Round(value.W));

        // Saturate
        public static float Saturate(float value) => Clamp(value, 0f, 1f);
        public static Vector2 Saturate(Vector2 value) => new Vector2(Clamp(value.X, 0f, 1f), Clamp(value.Y, 0f, 1f));
        public static Vector3 Saturate(Vector3 value) => new Vector3(Clamp(value.X, 0f, 1f), Clamp(value.Y, 0f, 1f), Clamp(value.Z, 0f, 1f));
        public static Vector4 Saturate(Vector4 value) => new Vector4(Clamp(value.X, 0f, 1f), Clamp(value.Y, 0f, 1f), Clamp(value.Z, 0f, 1f), Clamp(value.W, 0f, 1f));

        // Sin
        public static float Sin(float value) => (float)Math.Sin(value);
        public static Vector2 Sin(Vector2 value) => new Vector2((float)Math.Sin(value.X), (float)Math.Sin(value.Y));
        public static Vector3 Sin(Vector3 value) => new Vector3((float)Math.Sin(value.X), (float)Math.Sin(value.Y), (float)Math.Sin(value.Z));
        public static Vector4 Sin(Vector4 value) => new Vector4((float)Math.Sin(value.X), (float)Math.Sin(value.Y), (float)Math.Sin(value.Z), (float)Math.Sin(value.W));

        // Sinh
        public static float Sinh(float value) => (float)Math.Sinh(value);
        public static Vector2 Sinh(Vector2 value) => new Vector2((float)Math.Sinh(value.X), (float)Math.Sinh(value.Y));
        public static Vector3 Sinh(Vector3 value) => new Vector3((float)Math.Sinh(value.X), (float)Math.Sinh(value.Y), (float)Math.Sinh(value.Z));
        public static Vector4 Sinh(Vector4 value) => new Vector4((float)Math.Sinh(value.X), (float)Math.Sinh(value.Y), (float)Math.Sinh(value.Z), (float)Math.Sinh(value.W));

        // Sqrt
        public static float Sqrt(float value) => (float)Math.Sqrt(value);
        public static Vector2 Sqrt(Vector2 value) => new Vector2((float)Math.Sqrt(value.X), (float)Math.Sqrt(value.Y));
        public static Vector3 Sqrt(Vector3 value) => new Vector3((float)Math.Sqrt(value.X), (float)Math.Sqrt(value.Y), (float)Math.Sqrt(value.Z));
        public static Vector4 Sqrt(Vector4 value) => new Vector4((float)Math.Sqrt(value.X), (float)Math.Sqrt(value.Y), (float)Math.Sqrt(value.Z), (float)Math.Sqrt(value.W));

        // SmoothStep
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothStep(float min, float max, float x)
        {
            // From https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/smoothstep.xhtml
            /*
             * t = clamp((x - min) / (max - min), 0.0, 1.0);
             * return t * t * (3.0 - 2.0 * t);
             * Results are undefined if min ≥ max.
            */
            float t = Saturate((x - min) / (max - min));
            return t * t * (3f - 2f * t);
        }
        public static Vector2 SmoothStep(Vector2 min, Vector2 max, Vector2 x) => new Vector2(SmoothStep(min.X, max.X, x.X), SmoothStep(min.Y, max.Y, x.Y));
        public static Vector2 SmoothStep(float min, float max, Vector2 x) => new Vector2(SmoothStep(min, max, x.X), SmoothStep(min, max, x.Y));
        public static Vector3 SmoothStep(Vector3 min, Vector3 max, Vector3 x) => new Vector3(SmoothStep(min.X, max.X, x.X), SmoothStep(min.Y, max.Y, x.Y), SmoothStep(min.Z, max.Z, x.Z));
        public static Vector3 SmoothStep(float min, float max, Vector3 x) => new Vector3(SmoothStep(min, max, x.X), SmoothStep(min, max, x.Y), SmoothStep(min, max, x.Z));
        public static Vector4 SmoothStep(Vector4 min, Vector4 max, Vector4 x) => new Vector4(SmoothStep(min.X, max.X, x.X), SmoothStep(min.Y, max.Y, x.Y), SmoothStep(min.Z, max.Z, x.Z), SmoothStep(min.W, max.W, x.W));
        public static Vector4 SmoothStep(float min, float max, Vector4 x) => new Vector4(SmoothStep(min, max, x.X), SmoothStep(min, max, x.Y), SmoothStep(min, max, x.Z), SmoothStep(min, max, x.W));

        // Tan
        public static float Tan(float value) => (float)Math.Tan(value);
        public static Vector2 Tan(Vector2 value) => new Vector2((float)Math.Tan(value.X), (float)Math.Tan(value.Y));
        public static Vector3 Tan(Vector3 value) => new Vector3((float)Math.Tan(value.X), (float)Math.Tan(value.Y), (float)Math.Tan(value.Z));
        public static Vector4 Tan(Vector4 value) => new Vector4((float)Math.Tan(value.X), (float)Math.Tan(value.Y), (float)Math.Tan(value.Z), (float)Math.Tan(value.W));

        // Tanh
        public static float Tanh(float value) => (float)Math.Tanh(value);
        public static Vector2 Tanh(Vector2 value) => new Vector2((float)Math.Tanh(value.X), (float)Math.Tanh(value.Y));
        public static Vector3 Tanh(Vector3 value) => new Vector3((float)Math.Tanh(value.X), (float)Math.Tanh(value.Y), (float)Math.Tanh(value.Z));
        public static Vector4 Tanh(Vector4 value) => new Vector4((float)Math.Tanh(value.X), (float)Math.Tanh(value.Y), (float)Math.Tanh(value.Z), (float)Math.Tanh(value.W));

        // Truncate
        public static float Truncate(float value) => (float)Math.Truncate(value);
        public static Vector2 Truncate(Vector2 value) => new Vector2((float)Math.Truncate(value.X), (float)Math.Truncate(value.Y));
        public static Vector3 Truncate(Vector3 value) => new Vector3((float)Math.Truncate(value.X), (float)Math.Truncate(value.Y), (float)Math.Truncate(value.Z));
        public static Vector4 Truncate(Vector4 value) => new Vector4((float)Math.Truncate(value.X), (float)Math.Truncate(value.Y), (float)Math.Truncate(value.Z), (float)Math.Truncate(value.W));
    }
}
