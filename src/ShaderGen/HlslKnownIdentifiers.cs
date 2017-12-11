using System.Collections.Generic;

namespace ShaderGen
{
    public static class HlslKnownIdentifiers
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
                { "M12", "[0][1]" },
                { "M13", "[0][2]" },
                { "M14", "[0][3]" },
                { "M21", "[1][0]" },
                { "M22", "[1][1]" },
                { "M23", "[1][2]" },
                { "M24", "[1][3]" },
                { "M31", "[2][0]" },
                { "M32", "[2][1]" },
                { "M33", "[2][2]" },
                { "M34", "[2][3]" },
                { "M41", "[3][0]" },
                { "M42", "[3][1]" },
                { "M43", "[3][2]" },
                { "M44", "[3][3]" },
            };
            ret.Add("System.Numerics.Matrix4x4", m4x4Mappings);

            Dictionary<string, string> uint3Mappings = new Dictionary<string, string>()
            {
                { "X", "x" },
                { "Y", "y" },
                { "Z", "z" },
            };
            ret.Add("ShaderGen.UInt3", uint3Mappings);

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
