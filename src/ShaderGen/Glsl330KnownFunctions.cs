using System;
using System.Collections.Generic;

namespace ShaderGen
{
    public static class Glsl330KnownFunctions
    {
        private static Dictionary<string, Dictionary<string, InvocationTranslator>> s_mappings = GetMappings();

        private static Dictionary<string, Dictionary<string, InvocationTranslator>> GetMappings()
        {
            Dictionary<string, Dictionary<string, InvocationTranslator>> ret = new Dictionary<string, Dictionary<string, InvocationTranslator>>();

            Dictionary<string, InvocationTranslator> builtinMappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Abs", SimpleNameTranslator("abs") },
                { "Pow", SimpleNameTranslator("pow") },
                { "Acos", SimpleNameTranslator("acos") },
                { "Tan", SimpleNameTranslator("tan") },
                { "Clamp", SimpleNameTranslator("clamp") },
                { "Mul", MatrixMul },
                { "Sample", Sample2D },
                { "Discard", Discard },
                { "Saturate", Saturate },

            };
            ret.Add("ShaderGen.ShaderBuiltins", builtinMappings);

            Dictionary<string, InvocationTranslator> v3Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Normalize", SimpleNameTranslator("normalize") },
                { "Dot", SimpleNameTranslator("dot") },
                { "Distance", SimpleNameTranslator("distance") },
                { "Reflect", SimpleNameTranslator("reflect") },
            };
            ret.Add("System.Numerics.Vector3", v3Mappings);

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

            throw new ShaderGenerationException($"Reference to unknown function: {type}.{method}");
        }

        private static InvocationTranslator SimpleNameTranslator(string nameTarget)
        {
            return (type, method, parameters) =>
            {
                return $"{nameTarget}({InvocationParameterInfo.GetInvocationParameterList(parameters)})";
            };
        }

        private static string MatrixMul(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"{parameters[0].Identifier} * {parameters[1].Identifier}";
        }

        private static string Sample2D(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"texture({parameters[0].Identifier}, {parameters[2].Identifier})";
        }

        private static string Discard(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"discard;";
        }

        private static string Saturate(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters.Length == 1)
            {
                return $"clamp({parameters[0].Identifier}, 0, 1)";
            }
            else
            {
                throw new ShaderGenerationException("Unhandled number of arguments to ShaderBuiltins.Discard.");
            }
        }
    }
}
