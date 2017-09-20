using System.Collections.Generic;

namespace ShaderGen
{
    public static class GlslKnownTypes
    {
        private static readonly Dictionary<string, string> s_knownTypes = new Dictionary<string, string>()
        {
            { "System.Int32", "int" },
            { "System.Single", "float" },
            { "System.Numerics.Vector2", "vec2" },
            { "System.Numerics.Vector3", "vec3" },
            { "System.Numerics.Vector4", "vec4" },
            { "System.Numerics.Matrix4x4", "mat4" },
        };

        public static string GetMappedName(string name)
        {
            if (s_knownTypes.TryGetValue(name, out string mapped))
            {
                return mapped;
            }
            else
            {
                return name;
            }
        }
    }
}
