using System.Numerics;

namespace ShaderGen
{
    /// <summary>
    /// Members of this class can only be executed on the GPU.
    /// </summary>
    /// <remarks><para>Using any of the members of this class results in a <see cref="ShaderBuiltinException"/> being thrown.</para></remarks>
    public static class ShaderBuiltins
    {
        public static Vector4 Sample(Texture2DResource texture, SamplerResource sampler, Vector2 texCoords)
            => throw new ShaderBuiltinException();

        public static Vector4 Sample(TextureCubeResource texture, SamplerResource sampler, Vector3 texCoords)
            => throw new ShaderBuiltinException();

        public static Vector4 Sample(Texture2DArrayResource texture, SamplerResource sampler, Vector2 texCoords,
            uint arrayLayer)
            => throw new ShaderBuiltinException();

        public static Vector4 SampleGrad(Texture2DResource texture, SamplerResource sampler, Vector2 texCoords,
            Vector2 ddx, Vector2 ddy)
            => throw new ShaderBuiltinException();

        public static Vector4 SampleGrad(Texture2DArrayResource texture, SamplerResource sampler, Vector2 texCoords,
            uint arrayLayer, Vector2 ddx, Vector2 ddy)
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

        public static T Load<T>(RWTexture2DResource<T> texture, UInt2 texCoords) where T : struct =>
            throw new ShaderBuiltinException();

        public static T Store<T>(RWTexture2DResource<T> texture, UInt2 texCoords, T value) where T : struct =>
            throw new ShaderBuiltinException();

        public static float SampleComparisonLevelZero(DepthTexture2DResource texture, SamplerComparisonResource sampler,
            Vector2 texCoords, float compareValue)
            => throw new ShaderBuiltinException();

        public static float SampleComparisonLevelZero(DepthTexture2DArrayResource texture,
            SamplerComparisonResource sampler, Vector2 texCoords, uint arrayLayer, float compareValue)
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
        public static uint InterlockedAdd(AtomicBufferUInt32 buffer, uint index, uint value) =>
            throw new ShaderBuiltinException();

        public static uint InterlockedAdd(AtomicBufferUInt32 buffer, int index, uint value) =>
            throw new ShaderBuiltinException();

        public static int InterlockedAdd(AtomicBufferInt32 buffer, uint index, int value) =>
            throw new ShaderBuiltinException();

        public static int InterlockedAdd(AtomicBufferInt32 buffer, int index, int value) =>
            throw new ShaderBuiltinException();

        // Built-in variables
        public static uint VertexID => throw new ShaderBuiltinException();
        public static uint InstanceID => throw new ShaderBuiltinException();
        public static UInt3 DispatchThreadID => throw new ShaderBuiltinException();
        public static UInt3 GroupThreadID => throw new ShaderBuiltinException();
        public static bool IsFrontFace => throw new ShaderBuiltinException();

    }
}
