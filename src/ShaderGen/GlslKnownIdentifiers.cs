using System.Collections.Generic;

namespace ShaderGen
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
