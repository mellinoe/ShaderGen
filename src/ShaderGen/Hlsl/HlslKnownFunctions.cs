using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ShaderGen.Hlsl
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
                { "Cos", SimpleNameTranslator("cos") },
                { "Ddx", SimpleNameTranslator("ddx") },
                { "Ddy", SimpleNameTranslator("ddy") },
                { "Frac", SimpleNameTranslator("frac") },
                { "Lerp", SimpleNameTranslator("lerp") },
                { "Pow", SimpleNameTranslator("pow") },
                { "Acos", SimpleNameTranslator("acos") },
                { "Sin", SimpleNameTranslator("sin") },
                { "Tan", SimpleNameTranslator("tan") },
                { "Clamp", SimpleNameTranslator("clamp") },
                { "Mod", SimpleNameTranslator("fmod") },
                { "Sample", Sample },
                { "SampleGrad", SampleGrad },
                { "Load", Load },
                { "Discard", Discard },
                { nameof(ShaderBuiltins.ClipToTextureCoordinates), ClipToTextureCoordinates },
                { "VertexID", VertexID },
                { "InstanceID", InstanceID },
                { "DispatchThreadID", DispatchThreadID },
                { "GroupThreadID", GroupThreadID },
                { "IsFrontFace", IsFrontFace },
            };
            ret.Add("ShaderGen.ShaderBuiltins", new DictionaryTypeInvocationTranslator(builtinMappings));

            Dictionary<string, InvocationTranslator> v2Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Abs", SimpleNameTranslator("abs") },
                { "Add", BinaryOpTranslator("+") },
                { "Clamp", SimpleNameTranslator("clamp") },
                { "Cos", SimpleNameTranslator("cos") },
                { "Distance", SimpleNameTranslator("distance") },
                { "DistanceSquared", DistanceSquared },
                { "Divide", BinaryOpTranslator("/") },
                { "Dot", SimpleNameTranslator("dot") },
                { "Lerp", SimpleNameTranslator("lerp") },
                { "Max", SimpleNameTranslator("max") },
                { "Min", SimpleNameTranslator("min") },
                { "Multiply", BinaryOpTranslator("*") },
                { "Negate", Negate },
                { "Normalize", SimpleNameTranslator("normalize") },
                { "Reflect", SimpleNameTranslator("reflect") },
                { "Sin", SimpleNameTranslator("sin") },
                { "SquareRoot", SimpleNameTranslator("sqrt") },
                { "Subtract", BinaryOpTranslator("-") },
                { "Length", SimpleNameTranslator("length") },
                { "LengthSquared", LengthSquared },
                { "ctor", VectorCtor },
                { "Zero", VectorStaticAccessor },
                { "One", VectorStaticAccessor },
                { "UnitX", VectorStaticAccessor },
                { "UnitY", VectorStaticAccessor },
                { "Transform", Vector2Transform },
            };
            ret.Add("System.Numerics.Vector2", new DictionaryTypeInvocationTranslator(v2Mappings));

            Dictionary<string, InvocationTranslator> v3Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Abs", SimpleNameTranslator("abs") },
                { "Add", BinaryOpTranslator("+") },
                { "Clamp", SimpleNameTranslator("clamp") },
                { "Cos", SimpleNameTranslator("cos") },
                { "Cross", SimpleNameTranslator("cross") },
                { "Distance", SimpleNameTranslator("distance") },
                { "DistanceSquared", DistanceSquared },
                { "Divide", BinaryOpTranslator("/") },
                { "Dot", SimpleNameTranslator("dot") },
                { "Lerp", SimpleNameTranslator("lerp") },
                { "Max", SimpleNameTranslator("max") },
                { "Min", SimpleNameTranslator("min") },
                { "Multiply", BinaryOpTranslator("*") },
                { "Negate", Negate },
                { "Normalize", SimpleNameTranslator("normalize") },
                { "Reflect", SimpleNameTranslator("reflect") },
                { "Sin", SimpleNameTranslator("sin") },
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
                { "Transform", Vector3Transform },
            };
            ret.Add("System.Numerics.Vector3", new DictionaryTypeInvocationTranslator(v3Mappings));

            Dictionary<string, InvocationTranslator> v4Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Abs", SimpleNameTranslator("abs") },
                { "Add", BinaryOpTranslator("+") },
                { "Clamp", SimpleNameTranslator("clamp") },
                { "Cos", SimpleNameTranslator("cos") },
                { "Distance", SimpleNameTranslator("distance") },
                { "DistanceSquared", DistanceSquared },
                { "Divide", BinaryOpTranslator("/") },
                { "Dot", SimpleNameTranslator("dot") },
                { "Lerp", SimpleNameTranslator("lerp") },
                { "Max", SimpleNameTranslator("max") },
                { "Min", SimpleNameTranslator("min") },
                { "Multiply", BinaryOpTranslator("*") },
                { "Negate", Negate },
                { "Normalize", SimpleNameTranslator("normalize") },
                { "Reflect", SimpleNameTranslator("reflect") },
                { "Sin", SimpleNameTranslator("sin") },
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
                { "Transform", Vector4Transform },
            };
            ret.Add("System.Numerics.Vector4", new DictionaryTypeInvocationTranslator(v4Mappings));

            Dictionary<string, InvocationTranslator> m4x4Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { "ctor", MatrixCtor }
            };
            ret.Add("System.Numerics.Matrix4x4", new DictionaryTypeInvocationTranslator(m4x4Mappings));

            Dictionary<string, InvocationTranslator> mathfMappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Cos", SimpleNameTranslator("cos") },
                { "Max", SimpleNameTranslator("max") },
                { "Min", SimpleNameTranslator("min") },
                { "Pow", SimpleNameTranslator("pow") },
                { "Sin", SimpleNameTranslator("sin") },
            };
            ret.Add("System.MathF", new DictionaryTypeInvocationTranslator(mathfMappings));

            ret.Add("ShaderGen.ShaderSwizzle", new SwizzleTranslator());

            return ret;
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

        private static string MatrixCtor(string typeName, string methodName, InvocationParameterInfo[] p)
        {
            string paramList = string.Join(", ",
                p[0].Identifier, p[1].Identifier, p[2].Identifier, p[3].Identifier,
                p[4].Identifier, p[5].Identifier, p[6].Identifier, p[7].Identifier,
                p[8].Identifier, p[9].Identifier, p[10].Identifier, p[11].Identifier,
                p[12].Identifier, p[13].Identifier, p[14].Identifier, p[15].Identifier);

            return $"{{ {paramList} }}";
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

        private static InvocationTranslator BinaryOpTranslator(string op)
        {
            return (type, method, parameters) =>
            {
                return $"{parameters[0].Identifier} {op} {parameters[1].Identifier}";
            };
        }

        private static InvocationTranslator SimpleNameTranslator(string nameTarget)
        {
            return (type, method, parameters) =>
            {
                return $"{nameTarget}({InvocationParameterInfo.GetInvocationParameterList(parameters)})";
            };
        }

        private static string Sample(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName == "ShaderGen.Texture2DArrayResource")
            {
                return $"{parameters[0].Identifier}.Sample({parameters[1].Identifier}, float3({parameters[2].Identifier}, {parameters[3].Identifier}))";
            }
            else
            {
                return $"{parameters[0].Identifier}.Sample({parameters[1].Identifier}, {parameters[2].Identifier})";
            }
        }

        private static string SampleGrad(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName == "ShaderGen.Texture2DArrayResource")
            {
                return $"{parameters[0].Identifier}.SampleGrad({parameters[1].Identifier}, float3({parameters[2].Identifier}, {parameters[3].Identifier}), {parameters[4].Identifier}, {parameters[5].Identifier})";
            }
            else
            {
                return $"{parameters[0].Identifier}.SampleGrad({parameters[1].Identifier}, {parameters[2].Identifier}, {parameters[3].Identifier}, {parameters[4].Identifier})";
            }
        }

        private static string Load(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName == "ShaderGen.Texture2DResource")
            {
                return $"{parameters[0].Identifier}.Load(int3({parameters[2].Identifier}, {parameters[3].Identifier}))";
            }
            else
            {
                return $"{parameters[0].Identifier}.Load({parameters[2].Identifier}, {parameters[3].Identifier})";
            }
        }

        private static string Discard(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"discard;";
        }

        private static string ClipToTextureCoordinates(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            string target = parameters[0].Identifier;
            return $"float2(({target}.x / {target}.w) / 2 + 0.5, ({target}.y / {target}.w) / -2 + 0.5)";
        }

        private static string VertexID(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return "_builtins_VertexID";
        }

        private static string InstanceID(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return "_builtins_InstanceID";
        }

        private static string DispatchThreadID(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return "_builtins_DispatchThreadID";
        }

        private static string GroupThreadID(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return "_builtins_GroupThreadID";
        }

        private static string IsFrontFace(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return "_builtins_IsFrontFace";
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

        private static string Vector2Transform(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"mul({parameters[1].Identifier}, float4({parameters[0].Identifier}, 0, 1)).xy";
        }

        private static string Vector3Transform(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"mul({parameters[1].Identifier}, float4({parameters[0].Identifier}, 1)).xyz";
        }

        private static string Vector4Transform(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            string vecParam;
            if (parameters[0].FullTypeName == "System.Numerics.Vector2")
            {
                vecParam = $"float4({parameters[0].Identifier}, 0, 1)";
            }
            else if (parameters[0].FullTypeName == "System.Numerics.Vector3")
            {
                vecParam = $"float4({parameters[0].Identifier}, 1)";
            }
            else
            {
                vecParam = parameters[0].Identifier;
            }

            return $"mul({parameters[1].Identifier}, {vecParam})";
        }

        private static void GetVectorTypeInfo(string name, out string shaderType, out int elementCount)
        {
            if (name == "System.Numerics.Vector2") { shaderType = "float2"; elementCount = 2; }
            else if (name == "System.Numerics.Vector3") { shaderType = "float3"; elementCount = 3; }
            else if (name == "System.Numerics.Vector4") { shaderType = "float4"; elementCount = 4; }
            else { throw new ShaderGenerationException("VectorCtor translator was called on an invalid type: " + name); }
        }
    }

    public delegate string InvocationTranslator(string typeName, string methodName, InvocationParameterInfo[] parameters);
}
