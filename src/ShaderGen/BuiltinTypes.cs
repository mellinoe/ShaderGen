using System.Collections.Generic;

namespace ShaderGen
{
    public static class BuiltinTypes
    {
        private static readonly HashSet<string> s_builtins = new HashSet<string>()
        {
            "System.Single",
            "System.Numerics.Vector2",
            "System.Numerics.Vector3",
            "System.Numerics.Vector4",
            "System.Numerics.Matrix4x4",
        };

        public static bool IsBuiltinType(string fullTypeName)
        {
            return s_builtins.Contains(fullTypeName);
        }
    }
}
