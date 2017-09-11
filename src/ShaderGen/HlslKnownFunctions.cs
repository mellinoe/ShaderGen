using System;
using System.Collections.Generic;

namespace ShaderGen
{
    public static class HlslKnownFunctions
    {
        private static Dictionary<string, Dictionary<string, InvocationTranslator>> s_mappings = GetMappings();

        private static Dictionary<string, Dictionary<string, InvocationTranslator>> GetMappings()
        {
            Dictionary<string, Dictionary<string, InvocationTranslator>> ret = new Dictionary<string, Dictionary<string, InvocationTranslator>>();

            Dictionary<string, InvocationTranslator> builtinMappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Mul", SimpleNameTranslator("mul") },
                { "Sample", Sample2D }
            };

            ret.Add("ShaderGen.ShaderBuiltins", builtinMappings);

            return ret;
        }

        public static string TranslateInvocation(string type, string method, InvocationParameterInfo[] parameters)
        {
            if (s_mappings.TryGetValue(type, out var dict))
            {
                if (dict.TryGetValue(method, out InvocationTranslator mappedValue))
                {
                    return mappedValue(type, method, parameters);
                }
            }

            return method;
        }

        private static InvocationTranslator SimpleNameTranslator(string nameTarget)
        {
            return (type, method, parameters) =>
            {
                return $"{nameTarget}({InvocationParameterInfo.GetInvocationParameterList(parameters)})";
            };
        }

        private static string Sample2D(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"{parameters[0].Identifier}.Sample({parameters[1].Identifier}, {parameters[2].Identifier})";
        }
    }

    public delegate string InvocationTranslator(string typeName, string methodName, InvocationParameterInfo[] parameters);
}
