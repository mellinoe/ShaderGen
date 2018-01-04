using System.Collections.Generic;

namespace ShaderGen
{
    internal static class MetalKnownTypes
    {
        private static readonly Dictionary<string, string> s_knownTypes = new Dictionary<string, string>()
        {
            { "System.UInt32", "uint" },
            { "System.Int32", "int" },
            { "System.Single", "float" },
            { "System.Numerics.Vector2", "float2" },
            { "System.Numerics.Vector3", "float3" },
            { "System.Numerics.Vector4", "float4" },
            { "System.Numerics.Matrix4x4", "float4x4" },
            { "System.Void", "void" },
            { "ShaderGen.Texture2DResource", "texture2d<float>" },
            { "ShaderGen.TextureCubeResource", "texturecube<float>" },
            { "System.Boolean", "bool" },
            { "ShaderGen.UInt3", "uint3" },
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
