using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ShaderGen
{
    public static class Glsl330KnownFunctions
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

            Dictionary<string, InvocationTranslator> v2Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Abs", SimpleNameTranslator("abs") },
                { "Add", BinaryOpTranslator("+") },
                { "Clamp", SimpleNameTranslator("clamp") },
                { "Distance", SimpleNameTranslator("distance") },
                { "DistanceSquared", DistanceSquared },
                { "Divide", BinaryOpTranslator("/") },
                { "Dot", SimpleNameTranslator("dot") },
                { "Lerp", SimpleNameTranslator("mix") },
                { "Max", SimpleNameTranslator("max") },
                { "Min", SimpleNameTranslator("min") },
                { "Multiply", BinaryOpTranslator("*") },
                { "Negate", Negate },
                { "Normalize", SimpleNameTranslator("normalize") },
                { "Reflect", SimpleNameTranslator("reflect") },
                { "SquareRoot", SimpleNameTranslator("sqrt") },
                { "Subtract", BinaryOpTranslator("-") },
                { "Length", SimpleNameTranslator("length") },
                { "LengthSquared", LengthSquared },
                { "ctor", VectorCtor },
                { "Zero", VectorStaticAccessor },
                { "One", VectorStaticAccessor },
                { "UnitX", VectorStaticAccessor },
                { "UnitY", VectorStaticAccessor },
            };
            ret.Add("System.Numerics.Vector2", new DictionaryTypeInvocationTranslator(v2Mappings));

            Dictionary<string, InvocationTranslator> v3Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Abs", SimpleNameTranslator("abs") },
                { "Add", BinaryOpTranslator("+") },
                { "Clamp", SimpleNameTranslator("clamp") },
                { "Cross", SimpleNameTranslator("cross") },
                { "Distance", SimpleNameTranslator("distance") },
                { "DistanceSquared", DistanceSquared },
                { "Divide", BinaryOpTranslator("/") },
                { "Dot", SimpleNameTranslator("dot") },
                { "Lerp", SimpleNameTranslator("mix") },
                { "Max", SimpleNameTranslator("max") },
                { "Min", SimpleNameTranslator("min") },
                { "Multiply", BinaryOpTranslator("*") },
                { "Negate", Negate },
                { "Normalize", SimpleNameTranslator("normalize") },
                { "Reflect", SimpleNameTranslator("reflect") },
                { "SquareRoot", SimpleNameTranslator("sqrt") },
                { "Subtract", BinaryOpTranslator("-") },
                { "Length", SimpleNameTranslator("length") },
                { "LengthSquared", LengthSquared },
                { "ctor", VectorCtor },
                { "Zero", VectorStaticAccessor },
                { "One", VectorStaticAccessor },
                { "UnitX", VectorStaticAccessor },
                { "UnitY", VectorStaticAccessor },
                { "UnitZ", VectorStaticAccessor },
            };
            ret.Add("System.Numerics.Vector3", new DictionaryTypeInvocationTranslator(v3Mappings));

            Dictionary<string, InvocationTranslator> v4Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Abs", SimpleNameTranslator("abs") },
                { "Add", BinaryOpTranslator("+") },
                { "Clamp", SimpleNameTranslator("clamp") },
                { "Distance", SimpleNameTranslator("distance") },
                { "DistanceSquared", DistanceSquared },
                { "Divide", BinaryOpTranslator("/") },
                { "Dot", SimpleNameTranslator("dot") },
                { "Lerp", SimpleNameTranslator("mix") },
                { "Max", SimpleNameTranslator("max") },
                { "Min", SimpleNameTranslator("min") },
                { "Multiply", BinaryOpTranslator("*") },
                { "Negate", Negate },
                { "Normalize", SimpleNameTranslator("normalize") },
                { "Reflect", SimpleNameTranslator("reflect") },
                { "SquareRoot", SimpleNameTranslator("sqrt") },
                { "Subtract", BinaryOpTranslator("-") },
                { "Length", SimpleNameTranslator("length") },
                { "LengthSquared", LengthSquared },
                { "ctor", VectorCtor },
                { "Zero", VectorStaticAccessor },
                { "One", VectorStaticAccessor },
                { "UnitX", VectorStaticAccessor },
                { "UnitY", VectorStaticAccessor },
                { "UnitZ", VectorStaticAccessor },
                { "UnitW", VectorStaticAccessor },
            };
            ret.Add("System.Numerics.Vector4", new DictionaryTypeInvocationTranslator(v4Mappings));

            ret.Add("ShaderGen.ShaderSwizzle", new SwizzleTranslator());

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

        private static string LengthSquared(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"dot({parameters[0].Identifier}, {parameters[0].Identifier})";
        }

        private static string Negate(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"-{parameters[0].Identifier}";
        }

        private static string DistanceSquared(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"dot({parameters[0].Identifier} - {parameters[1].Identifier}, {parameters[0].Identifier} - {parameters[1].Identifier})";
        }

        private static InvocationTranslator BinaryOpTranslator(string op)
        {
            return (type, method, parameters) =>
            {
                return $"{parameters[0].Identifier} {op} {parameters[1].Identifier}";
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

        private static string ClipToTextureCoordinates(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            string target = parameters[0].Identifier;
            return $"vec2(({target}.x / {target}.w) / 2 + 0.5, ({target}.y / {target}.w) / 2 + 0.5)";
        }

        private static string VectorCtor(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            GetVectorTypeInfo(typeName, out string shaderType, out int elementCount);
            string paramList;
            if (parameters.Length == 0)
            {
                paramList = string.Join(", ", Enumerable.Repeat("0", elementCount));
            }
            else if (parameters.Length == 1)
            {
                paramList = string.Join(", ", Enumerable.Repeat(parameters[0].Identifier, elementCount));
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < parameters.Length; i++)
                {
                    InvocationParameterInfo ipi = parameters[i];
                    sb.Append(ipi.Identifier);

                    if (i != parameters.Length - 1)
                    {
                        sb.Append(", ");
                    }
                }

                paramList = sb.ToString();
            }

            return $"{shaderType}({paramList})";
        }

        private static string VectorStaticAccessor(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            Debug.Assert(parameters.Length == 0);
            GetVectorTypeInfo(typeName, out string shaderType, out int elementCount);
            if (methodName == "Zero")
            {
                return $"{shaderType}({string.Join(", ", Enumerable.Repeat("0", elementCount))})";
            }
            else if (methodName == "One")
            {
                return $"{shaderType}({string.Join(", ", Enumerable.Repeat("1", elementCount))})";
            }
            else if (methodName == "UnitX")
            {
                string paramList;
                if (elementCount == 2) { paramList = "1, 0"; }
                else if (elementCount == 3) { paramList = "1, 0, 0"; }
                else { paramList = "1, 0, 0, 0"; }
                return $"{shaderType}({paramList})";
            }
            else if (methodName == "UnitY")
            {
                string paramList;
                if (elementCount == 2) { paramList = "0, 1"; }
                else if (elementCount == 3) { paramList = "0, 1, 0"; }
                else { paramList = "0, 1, 0, 0"; }
                return $"{shaderType}({paramList})";
            }
            else if (methodName == "UnitZ")
            {
                string paramList;
                if (elementCount == 3) { paramList = "0, 0, 1"; }
                else { paramList = "0, 0, 1, 0"; }
                return $"{shaderType}({paramList})";
            }
            else if (methodName == "UnitW")
            {
                return $"{shaderType}(0, 0, 0, 1)";
            }
            else
            {
                Debug.Fail("Invalid static vector accessor: " + methodName);
                return null;
            }
        }

        private static void GetVectorTypeInfo(string name, out string shaderType, out int elementCount)
        {
            if (name == "System.Numerics.Vector2") { shaderType = "vec2"; elementCount = 2; }
            else if (name == "System.Numerics.Vector3") { shaderType = "vec3"; elementCount = 3; }
            else if (name == "System.Numerics.Vector4") { shaderType = "vec4"; elementCount = 4; }
            else { throw new ShaderGenerationException("VectorCtor translator was called on an invalid type: " + name); }
        }
    }
}
