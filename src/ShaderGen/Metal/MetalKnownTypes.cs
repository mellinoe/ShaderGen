using System.Collections.Generic;

namespace ShaderGen.Metal
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
            { "ShaderGen.Texture2DArrayResource", "texture2d_array<float>" },
            { "ShaderGen.TextureCubeResource", "texturecube<float>" },
            { "ShaderGen.SamplerResource", "sampler" },
            { "ShaderGen.SamplerComparisonResource", "sampler" },
            { "System.Boolean", "bool" },
            { "ShaderGen.UInt2", "uint2" },
            { "ShaderGen.UInt3", "uint3" },
            { "ShaderGen.UInt4", "uint4" },
            { "ShaderGen.Int2", "int2" },
            { "ShaderGen.Int3", "int3" },
            { "ShaderGen.Int4", "int4" },
        };

        private static readonly Dictionary<string, string> s_mappedToPackedTypes = new Dictionary<string, string>()
        {
            { "float2", "packed_float2" },
            { "float3", "packed_float3" },
            { "float4", "packed_float4" },
            { "uint2", "packed_uint2" },
            { "uint3", "packed_uint3" },
            { "uint4", "packed_uint4" },
            { "int2", "packed_int2" },
            { "int3", "packed_int3" },
            { "int4", "packed_int4" },
        };

        private static readonly Dictionary<string, string> s_csharpToUnpackedTypes = new Dictionary<string, string>()
        {
            { "System.Numerics.Vector2", "float2" },
            { "System.Numerics.Vector3", "float3" },
            { "System.Numerics.Vector4", "float4" },
            { "ShaderGen.UInt2", "uint2" },
            { "ShaderGen.UInt3", "uint3" },
            { "ShaderGen.UInt4", "uint4" },
            { "ShaderGen.Int2", "int2" },
            { "ShaderGen.Int3", "int3" },
            { "ShaderGen.Int4", "int4" },
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

        public static string GetPackedName(string name)
        {
            string mappedName = GetMappedName(name);
            if (s_mappedToPackedTypes.TryGetValue(mappedName, out string packed))
            {
                return packed;
            }
            else
            {
                return mappedName;
            }
        }

        internal static bool GetUnpackedType(string typeName, out string unpackCast)
        {
            return s_csharpToUnpackedTypes.TryGetValue(typeName, out unpackCast);
        }
    }
}
