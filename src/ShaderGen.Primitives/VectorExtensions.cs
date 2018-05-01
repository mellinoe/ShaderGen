using System.Numerics;

namespace ShaderGen
{
    public static class VectorExtensions
    {
        // C# doesn't have extension indexers (yet - https://github.com/dotnet/roslyn/issues/11159)
        // so this is the best we can do.
        public static float GetComponent(this Vector2 vector, int index) => throw new ShaderBuiltinException();
        public static float GetComponent(this Vector3 vector, int index) => throw new ShaderBuiltinException();
        public static float GetComponent(this Vector4 vector, int index) => throw new ShaderBuiltinException();
        public static void SetComponent(this Vector2 vector, int index, float value) => throw new ShaderBuiltinException();
        public static void SetComponent(this Vector3 vector, int index, float value) => throw new ShaderBuiltinException();
        public static void SetComponent(this Vector4 vector, int index, float value) => throw new ShaderBuiltinException();
    }
}
