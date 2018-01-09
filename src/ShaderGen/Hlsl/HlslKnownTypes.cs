using System.Collections.Generic;

namespace ShaderGen.Hlsl
{
    internal static class HlslKnownTypes
    {
        private static readonly Dictionary<string, string> KnownTypes = new Dictionary<string, string>()
        {
            { "System.UInt32", "uint" },
            { "System.Int32", "int" },
            { "System.Single", "float" },
            { "System.Numerics.Vector2", "float2" },
            { "System.Numerics.Vector3", "float3" },
            { "System.Numerics.Vector4", "float4" },
            { "System.Numerics.Matrix4x4", "float4x4" },
            { "System.Void", "void" },
            { "ShaderGen.Texture2DResource", "Texture2D" },
            { "ShaderGen.TextureCubeResource", "TextureCube" },
            { "System.Boolean", "bool" },
            { "ShaderGen.UInt2", "uint2" },
            { "ShaderGen.UInt3", "uint3" },
            { "ShaderGen.UInt4", "uint4" },
            { "ShaderGen.Int2", "int2" },
            { "ShaderGen.Int3", "int3" },
            { "ShaderGen.Int4", "int4" },
        };

        public static string GetMappedName(string name)
        {
            if (KnownTypes.TryGetValue(name, out string mapped))
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
