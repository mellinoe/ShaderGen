using System.Numerics;

namespace ShaderGen
{
    public static class VectorExtensions
    {
        // C# doesn't have extension indexers (yet - https://github.com/dotnet/roslyn/issues/11159)
        // so this is the best we can do.
        public static float Item(this Vector2 value, int index) => throw new ShaderBuiltinException();
        public static float Item(this Vector3 value, int index) => throw new ShaderBuiltinException();
        public static float Item(this Vector4 value, int index) => throw new ShaderBuiltinException();
    }
}
