using System;
using System.Collections.Generic;

namespace ShaderGen
{
    public static class Glsl450KnownFunctions
    {
        private static Dictionary<string, TypeInvocationTranslator> s_mappings = GetMappings();

        private static Dictionary<string, TypeInvocationTranslator> GetMappings()
        {
            Dictionary<string, TypeInvocationTranslator> ret = new Dictionary<string, TypeInvocationTranslator>();

            Dictionary<string, InvocationTranslator> builtinMappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Abs", SimpleNameTranslator("abs") },
                { "Pow", SimpleNameTranslator("pow") },
                { "Acos", SimpleNameTranslator("acos") },
                { "Tan", SimpleNameTranslator("tan") },
                { "Clamp", SimpleNameTranslator("clamp") },
                { "Mod", SimpleNameTranslator("mod") },
                { "Mul", MatrixMul },
                { "Sample", Sample2D },
                { "Discard", Discard },
                { "Saturate", Saturate },
                { nameof(ShaderBuiltins.ClipToTextureCoordinates), ClipToTextureCoordinates },
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

            ret.Add("ShaderGen.ShaderSwizzle", new SwizzleTranslator());

            return ret;
        }

        public static string TranslateInvocation(string type, string method, InvocationParameterInfo[] parameters)
        {
            if (s_mappings.TryGetValue(type, out var dict))
            {
                if (dict.GetTranslator(method, parameters, out var mappedValue))
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
            if (parameters[0].FullTypeName == "ShaderGen.Texture2DResource")
            {
                return $"texture(sampler2D({parameters[0].Identifier}, {parameters[1].Identifier}), {parameters[2].Identifier})";
            }
            else if (parameters[0].FullTypeName == "ShaderGen.TextureCubeResource")
            {
                return $"texture(samplerCube({parameters[0].Identifier}, {parameters[1].Identifier}), {parameters[2].Identifier})";
            }
            else
            {
                throw new NotImplementedException();
            }
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

        private static string ClipToTextureCoordinates(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            string target = parameters[0].Identifier;
            return $"vec2({target}.x / 2 + 0.5, {target}.y / -2 + 0.5)";
        }
    }
}
