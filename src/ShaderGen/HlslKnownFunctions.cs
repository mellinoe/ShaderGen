using System.Collections.Generic;

namespace ShaderGen
{
    public static class HlslKnownFunctions
    {
        private static Dictionary<string, Dictionary<string, string>> s_mappings = GetMappings();

        private static Dictionary<string, Dictionary<string, string>> GetMappings()
        {
            Dictionary<string, Dictionary<string, string>> ret = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> builtinMappings = new Dictionary<string, string>()
            {
                { "Mul", "mul" }
            };

            ret.Add("ShaderGen.ShaderBuiltins", builtinMappings);

            return ret;
        }

        public static string GetMappedFunctionName(string type, string method)
        {
            if (s_mappings.TryGetValue(type, out var dict))
            {
                if (dict.TryGetValue(method, out string mappedValue))
                {
                    return mappedValue;
                }
            }

            return method;
        }
    }
}
