using System.Collections.Generic;

namespace ShaderGen.Glsl
{
    public static class GlslKnownIdentifiers
    {
        private static Dictionary<string, Dictionary<string, string>> s_mappings = GetMappings();

        private static Dictionary<string, Dictionary<string, string>> GetMappings()
        {
            Dictionary<string, Dictionary<string, string>> ret = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> v2Mappings = new Dictionary<string, string>()
            {
                { "X", "x" },
                { "Y", "y" },
            };
            ret.Add("System.Numerics.Vector2", v2Mappings);

            Dictionary<string, string> v3Mappings = new Dictionary<string, string>()
            {
                { "X", "x" },
                { "Y", "y" },
                { "Z", "z" },
            };
            ret.Add("System.Numerics.Vector3", v3Mappings);

            Dictionary<string, string> v4Mappings = new Dictionary<string, string>()
            {
                { "X", "x" },
                { "Y", "y" },
                { "Z", "z" },
                { "W", "w" },
            };
            ret.Add("System.Numerics.Vector4", v4Mappings);

            Dictionary<string, string> m4x4Mappings = new Dictionary<string, string>()
            {
                { "M11", "[0][0]" },
                { "M12", "[1][0]" },
                { "M13", "[2][0]" },
                { "M14", "[3][0]" },
                { "M21", "[0][1]" },
                { "M22", "[1][1]" },
                { "M23", "[2][1]" },
                { "M24", "[3][1]" },
                { "M31", "[0][2]" },
                { "M32", "[1][2]" },
                { "M33", "[2][2]" },
                { "M34", "[3][2]" },
                { "M41", "[0][3]" },
                { "M42", "[1][3]" },
                { "M43", "[2][3]" },
                { "M44", "[3][3]" },
            };
            ret.Add("System.Numerics.Matrix4x4", m4x4Mappings);

            Dictionary<string, string> uint2Mappings = new Dictionary<string, string>()
            {
                { "X", "x" },
                { "Y", "y" },
            };
            ret.Add("ShaderGen.UInt2", uint2Mappings);

            Dictionary<string, string> uint3Mappings = new Dictionary<string, string>()
            {
                { "X", "x" },
                { "Y", "y" },
                { "Z", "z" },
            };
            ret.Add("ShaderGen.UInt3", uint3Mappings);

            Dictionary<string, string> uint4Mappings = new Dictionary<string, string>()
            {
                { "X", "x" },
                { "Y", "y" },
                { "Z", "z" },
                { "W", "w" },
            };
            ret.Add("ShaderGen.UInt4", uint4Mappings);

            Dictionary<string, string> int2Mappings = new Dictionary<string, string>()
            {
                { "X", "x" },
                { "Y", "y" },
            };
            ret.Add("ShaderGen.Int2", int2Mappings);

            Dictionary<string, string> int3Mappings = new Dictionary<string, string>()
            {
                { "X", "x" },
                { "Y", "y" },
                { "Z", "z" },
            };
            ret.Add("ShaderGen.Int3", int3Mappings);

            Dictionary<string, string> int4Mappings = new Dictionary<string, string>()
            {
                { "X", "x" },
                { "Y", "y" },
                { "Z", "z" },
                { "W", "w" },
            };
            ret.Add("ShaderGen.Int4", int4Mappings);

            return ret;
        }

        public static string GetMappedIdentifier(string type, string identifier)
        {
            if (s_mappings.TryGetValue(type, out var dict))
            {
                if (dict.TryGetValue(identifier, out string mappedValue))
                {
                    return mappedValue;
                }
            }

            return identifier;
        }
    }
}
