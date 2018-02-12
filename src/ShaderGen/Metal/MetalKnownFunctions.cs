using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ShaderGen.Hlsl;

namespace ShaderGen.Metal
{
    public static class MetalKnownFunctions
    {
        private static Dictionary<string, TypeInvocationTranslator> s_mappings = GetMappings();

        private static Dictionary<string, TypeInvocationTranslator> GetMappings()
        {
            Dictionary<string, TypeInvocationTranslator> ret = new Dictionary<string, TypeInvocationTranslator>();

            Dictionary<string, InvocationTranslator> builtinMappings = new Dictionary<string, InvocationTranslator>()
            {
                { "Mul", MatrixMul },
                { "Saturate", SimpleNameTranslator("saturate") },
                { "Abs", SimpleNameTranslator("abs") },
                { "Pow", Pow },
                { "Acos", SimpleNameTranslator("acos") },
                { "Cos", SimpleNameTranslator("cos") },
                { "Frac", SimpleNameTranslator("fract") },
                { "Lerp", SimpleNameTranslator("mix") },
                { "Sin", SimpleNameTranslator("sin") },
                { "Tan", SimpleNameTranslator("tan") },
                { "Clamp", Clamp },
                { "Mod", SimpleNameTranslator("fmod") },
                { "Sample", Sample2D },
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
                { "Lerp", SimpleNameTranslator("mix") },
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
                { "Lerp", SimpleNameTranslator("mix") },
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
                { "Lerp", SimpleNameTranslator("mix") },
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
                { "Pow", Pow },
                { "Sin", SimpleNameTranslator("sin") },
            };
            ret.Add("System.MathF", new DictionaryTypeInvocationTranslator(mathfMappings));

            ret.Add("ShaderGen.ShaderSwizzle", new MetalSwizzleTranslator());

            return ret;
        }

        private static string Clamp(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName == "float")
            {
                return $"clamp({parameters[0].Identifier}, (float){parameters[1].Identifier}, (float){parameters[2].Identifier})";
            }
            else
            {
                return SimpleNameTranslator("clamp")(typeName, methodName, parameters);
            }
        }

        private static string Pow(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName == "float" || typeName == "System.MathF")
            {
                return $"pow({parameters[0].Identifier}, (float){parameters[1].Identifier})";
            }
            else
            {
                return SimpleNameTranslator("pow")(typeName, methodName, parameters);
            }
        }

        private static string MatrixMul(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"{parameters[0].Identifier} * float4({parameters[1].Identifier})";
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
                $"float4({p[0].Identifier}, {p[1].Identifier}, {p[2].Identifier}, {p[3].Identifier})",
                $"float4({p[4].Identifier}, {p[5].Identifier}, {p[6].Identifier}, {p[7].Identifier})",
                $"float4({p[8].Identifier}, {p[9].Identifier}, {p[10].Identifier}, {p[11].Identifier})",
                $"float4({p[12].Identifier}, {p[13].Identifier}, {p[14].Identifier}, {p[15].Identifier})");

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

        private static string Sample2D(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"{parameters[0].Identifier}.sample({parameters[1].Identifier}, {parameters[2].Identifier})";
        }

        private static string Load(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"{parameters[0].Identifier}.read(uint2({parameters[2].Identifier}.x, {parameters[2].Identifier}.y), {parameters[3].Identifier})";
        }

        private static string Discard(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"discard_fragment();";
        }

        private static string ClipToTextureCoordinates(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            string target = parameters[0].Identifier;
            return $"float2(({target}[0] / {target}[3]) / 2 + 0.5, ({target}[1] / {target}[3]) / -2 + 0.5)";
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
            return $"({parameters[1].Identifier} * float4({parameters[0].Identifier}, 0, 1)).xy";
        }

        private static string Vector3Transform(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"({parameters[1].Identifier} * float4({parameters[0].Identifier}, 1)).xyz";
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
                vecParam = $"float4({parameters[0].Identifier})";
            }

            return $"{parameters[1].Identifier} * {vecParam}";
        }

        private static void GetVectorTypeInfo(string name, out string shaderType, out int elementCount)
        {
            if (name == "System.Numerics.Vector2") { shaderType = "float2"; elementCount = 2; }
            else if (name == "System.Numerics.Vector3") { shaderType = "float3"; elementCount = 3; }
            else if (name == "System.Numerics.Vector4") { shaderType = "float4"; elementCount = 4; }
            else { throw new ShaderGenerationException("VectorCtor translator was called on an invalid type: " + name); }
        }
    }
}
