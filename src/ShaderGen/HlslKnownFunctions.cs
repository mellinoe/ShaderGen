using System;
using System.Collections.Generic;

namespace ShaderGen
{
    public static class HlslKnownFunctions
    {
        private static Dictionary<string, TypeInvocationTranslator> s_mappings = GetMappings();

        private static Dictionary<string, TypeInvocationTranslator> GetMappings()
        {
            Dictionary<string, TypeInvocationTranslator> ret = new Dictionary<string, TypeInvocationTranslator>();

            Dictionary<string, InvocationTranslator> builtinMappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Mul", SimpleNameTranslator("mul") },
                { "Saturate", SimpleNameTranslator("saturate") },
                { "Abs", SimpleNameTranslator("abs") },
                { "Pow", SimpleNameTranslator("pow") },
                { "Acos", SimpleNameTranslator("acos") },
                { "Tan", SimpleNameTranslator("tan") },
                { "Clamp", SimpleNameTranslator("clamp") },
                { "Sample", Sample2D },
                { "Discard", Discard },
            };
            ret.Add("ShaderGen.ShaderBuiltins", new DictionaryTypeInvocationTranslator(builtinMappings));

            Dictionary<string, InvocationTranslator> v3Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Normalize", SimpleNameTranslator("normalize") },
                { "Dot", SimpleNameTranslator("dot") },
                { "Distance", SimpleNameTranslator("distance") },
                { "Reflect", SimpleNameTranslator("reflect") },
            };
            ret.Add("System.Numerics.Vector3", new DictionaryTypeInvocationTranslator(v3Mappings));

            return ret;
        }


        public static string TranslateInvocation(string type, string method, InvocationParameterInfo[] parameters)
        {
            if (s_mappings.TryGetValue(type, out var dict))
            {
                if (dict.GetTranslator(method, parameters, out InvocationTranslator mappedValue))
                {
                    return mappedValue(type, method, parameters);
                }
            }

            throw new ShaderGenerationException($"Reference to unknown function: {type}.{method}");
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

        private static string Discard(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"discard;";
        }
    }

    public delegate string InvocationTranslator(string typeName, string methodName, InvocationParameterInfo[] parameters);
}
