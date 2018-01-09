using System.Collections.Generic;

namespace ShaderGen.Glsl
{
    internal static class GlslKnownTypes
    {
        private static readonly Dictionary<string, string> KnownTypesShared = new Dictionary<string, string>()
        {
            { "System.UInt32", "uint" },
            { "System.Int32", "int" },
            { "System.Single", "float" },
            { "System.Numerics.Vector2", "vec2" },
            { "System.Numerics.Vector3", "vec3" },
            { "System.Numerics.Vector4", "vec4" },
            { "System.Numerics.Matrix4x4", "mat4" },
            { "System.Void", "void" },
            { "System.Boolean", "bool" },
            { "ShaderGen.UInt2", "uvec2" },
            { "ShaderGen.UInt3", "uvec3" },
            { "ShaderGen.UInt4", "uvec4" },
            { "ShaderGen.Int2", "ivec2" },
            { "ShaderGen.Int3", "ivec3" },
            { "ShaderGen.Int4", "ivec4" },

        };

        private static readonly Dictionary<string, string> KnownTypesGl = new Dictionary<string, string>()
        {
            { "ShaderGen.Texture2DResource", "sampler2D" },
            { "ShaderGen.TextureCubeResource", "samplerCube" },
        };


        private static readonly Dictionary<string, string> KnownTypesVulkan = new Dictionary<string, string>()
        {
            { "ShaderGen.Texture2DResource", "texture2D" },
            { "ShaderGen.TextureCubeResource", "textureCube" },
        };


        public static string GetMappedName(string name, bool vulkan)
        {
            if (KnownTypesShared.TryGetValue(name, out string mapped))
            {
                return mapped;
            }
            else if (vulkan)
            {
                if (KnownTypesVulkan.TryGetValue(name, out mapped))
                {
                    return mapped;
                }
            }
            else
            {
                if (KnownTypesGl.TryGetValue(name, out mapped))
                {
                    return mapped;
                }
            }

            return name;
        }
    }
}
