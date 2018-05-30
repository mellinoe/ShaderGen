using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
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
                { nameof(ShaderBuiltins.Abs), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Acos), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Acosh), Acosh },
                { nameof(ShaderBuiltins.Asin), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Asinh), Asinh },
                { nameof(ShaderBuiltins.Atan), Atan },// Note atan supports both (x) and (y,x)
                { nameof(ShaderBuiltins.Atanh), Atanh },
                { nameof(ShaderBuiltins.Cbrt), CubeRoot }, // We can calculate the 1/3rd power, which might not give exactly the same result?
                { nameof(ShaderBuiltins.Ceiling), SimpleNameTranslator("ceil") },
                { nameof(ShaderBuiltins.Clamp), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.ClipToTextureCoordinates), ClipToTextureCoordinates },
                { nameof(ShaderBuiltins.Cos), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Cosh), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Ddx), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.DdxFine), SimpleNameTranslator("ddx_fine") },
                { nameof(ShaderBuiltins.Ddy), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.DdyFine), SimpleNameTranslator("ddy_fine") },
                { nameof(ShaderBuiltins.Discard), Discard },
                { nameof(ShaderBuiltins.DispatchThreadID), DispatchThreadID },
                { nameof(ShaderBuiltins.Exp), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Floor), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Frac), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.GroupThreadID), GroupThreadID },
                { nameof(ShaderBuiltins.InstanceID), InstanceID },
                { nameof(ShaderBuiltins.InterlockedAdd), InterlockedAdd },
                { nameof(ShaderBuiltins.IsFrontFace), IsFrontFace },
                { nameof(ShaderBuiltins.Lerp), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Load), Load },
                { nameof(ShaderBuiltins.Log), Log },
                { nameof(ShaderBuiltins.Log2), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Log10), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Max), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Min), SimpleNameTranslator() },
                // Potential BUG: https://stackoverflow.com/questions/7610631/glsl-mod-vs-hlsl-fmod
                { nameof(ShaderBuiltins.Mod), SimpleNameTranslator("fmod") },
                { nameof(ShaderBuiltins.Mul), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Pow), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Round), Round },
                { nameof(ShaderBuiltins.Sample), Sample },
                { nameof(ShaderBuiltins.SampleComparisonLevelZero), SampleComparisonLevelZero },
                { nameof(ShaderBuiltins.SampleGrad), SampleGrad },
                { nameof(ShaderBuiltins.Saturate), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Sin), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Sinh), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.SmoothStep), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Sqrt), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Store), Store },
                { nameof(ShaderBuiltins.Tan), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Tanh), SimpleNameTranslator() },
                { nameof(ShaderBuiltins.Truncate), SimpleNameTranslator("trunc") },
                { nameof(ShaderBuiltins.VertexID), VertexID }
            };
            ret.Add("ShaderGen.ShaderBuiltins", new DictionaryTypeInvocationTranslator(builtinMappings));

            Dictionary<string, InvocationTranslator> v2Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { ".ctor", VectorCtor },
                { nameof(Vector2.Abs), SimpleNameTranslator("abs") },
                { nameof(Vector2.Add), BinaryOpTranslator("+") },
                { nameof(Vector2.Clamp), SimpleNameTranslator("clamp") },
                // Doesn't exist! { nameof(Vector2.Cos), SimpleNameTranslator("cos") },
                { nameof(Vector2.Distance), SimpleNameTranslator("distance") },
                { nameof(Vector2.DistanceSquared), DistanceSquared },
                { nameof(Vector2.Divide), BinaryOpTranslator("/") },
                { nameof(Vector2.Dot), SimpleNameTranslator("dot") },
                { nameof(Vector2.Lerp), SimpleNameTranslator("lerp") },
                { nameof(Vector2.Max), SimpleNameTranslator("max") },
                { nameof(Vector2.Min), SimpleNameTranslator("min") },
                { nameof(Vector2.Multiply), BinaryOpTranslator("*") },
                { nameof(Vector2.Negate), Negate },
                { nameof(Vector2.Normalize), SimpleNameTranslator("normalize") },
                { nameof(Vector2.Reflect), SimpleNameTranslator("reflect") },
                // Doesn't exist! { nameof(Vector2.Sin), SimpleNameTranslator("sin") },
                { nameof(Vector2.SquareRoot), SimpleNameTranslator("sqrt") },
                { nameof(Vector2.Subtract), BinaryOpTranslator("-") },
                { nameof(Vector2.Length), SimpleNameTranslator("length") },
                { nameof(Vector2.LengthSquared), LengthSquared },
                { nameof(Vector2.Zero), VectorStaticAccessor },
                { nameof(Vector2.One), VectorStaticAccessor },
                { nameof(Vector2.UnitX), VectorStaticAccessor },
                { nameof(Vector2.UnitY), VectorStaticAccessor },
                { nameof(Vector2.Transform), Vector2Transform }
            };
            ret.Add("System.Numerics.Vector2", new DictionaryTypeInvocationTranslator(v2Mappings));

            Dictionary<string, InvocationTranslator> v3Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { ".ctor", VectorCtor },
                { nameof(Vector3.Abs), SimpleNameTranslator("abs") },
                { nameof(Vector3.Add), BinaryOpTranslator("+") },
                { nameof(Vector3.Clamp), SimpleNameTranslator("clamp") },
                // Doesn't exist! { nameof(Vector3.Cos), SimpleNameTranslator("cos") },
                { nameof(Vector3.Cross), SimpleNameTranslator("cross") },
                { nameof(Vector3.Distance), SimpleNameTranslator("distance") },
                { nameof(Vector3.DistanceSquared), DistanceSquared },
                { nameof(Vector3.Divide), BinaryOpTranslator("/") },
                { nameof(Vector3.Dot), SimpleNameTranslator("dot") },
                { nameof(Vector3.Lerp), SimpleNameTranslator("lerp") },
                { nameof(Vector3.Max), SimpleNameTranslator("max") },
                { nameof(Vector3.Min), SimpleNameTranslator("min") },
                { nameof(Vector3.Multiply), BinaryOpTranslator("*") },
                { nameof(Vector3.Negate), Negate },
                { nameof(Vector3.Normalize), SimpleNameTranslator("normalize") },
                { nameof(Vector3.Reflect), SimpleNameTranslator("reflect") },
                // Doesn't exist! { nameof(Vector3.Sin), SimpleNameTranslator("sin") },
                { nameof(Vector3.SquareRoot), SimpleNameTranslator("sqrt") },
                { nameof(Vector3.Subtract), BinaryOpTranslator("-") },
                { nameof(Vector3.Length), SimpleNameTranslator("length") },
                { nameof(Vector3.LengthSquared), LengthSquared },
                { nameof(Vector3.Zero), VectorStaticAccessor },
                { nameof(Vector3.One), VectorStaticAccessor },
                { nameof(Vector3.UnitX), VectorStaticAccessor },
                { nameof(Vector3.UnitY), VectorStaticAccessor },
                { nameof(Vector3.UnitZ), VectorStaticAccessor },
                { nameof(Vector3.Transform), Vector3Transform }
            };
            ret.Add("System.Numerics.Vector3", new DictionaryTypeInvocationTranslator(v3Mappings));

            Dictionary<string, InvocationTranslator> v4Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { ".ctor", VectorCtor },
                { nameof(Vector4.Abs), SimpleNameTranslator("abs") },
                { nameof(Vector4.Add), BinaryOpTranslator("+") },
                { nameof(Vector4.Clamp), SimpleNameTranslator("clamp") },
                // Doesn't exist! { nameof(Vector4.Cos), SimpleNameTranslator("cos") },
                { nameof(Vector4.Distance), SimpleNameTranslator("distance") },
                { nameof(Vector4.DistanceSquared), DistanceSquared },
                { nameof(Vector4.Divide), BinaryOpTranslator("/") },
                { nameof(Vector4.Dot), SimpleNameTranslator("dot") },
                { nameof(Vector4.Lerp), SimpleNameTranslator("lerp") },
                { nameof(Vector4.Max), SimpleNameTranslator("max") },
                { nameof(Vector4.Min), SimpleNameTranslator("min") },
                { nameof(Vector4.Multiply), BinaryOpTranslator("*") },
                { nameof(Vector4.Negate), Negate },
                { nameof(Vector4.Normalize), SimpleNameTranslator("normalize") },
                // Doesn't exist! { nameof(Vector4.Reflect), SimpleNameTranslator("reflect") },
                // Doesn't exist! { nameof(Vector4.Sin), SimpleNameTranslator("sin") },
                { nameof(Vector4.SquareRoot), SimpleNameTranslator("sqrt") },
                { nameof(Vector4.Subtract), BinaryOpTranslator("-") },
                { nameof(Vector4.Length), SimpleNameTranslator("length") },
                { nameof(Vector4.LengthSquared), LengthSquared },
                { nameof(Vector4.Zero), VectorStaticAccessor },
                { nameof(Vector4.One), VectorStaticAccessor },
                { nameof(Vector4.UnitX), VectorStaticAccessor },
                { nameof(Vector4.UnitY), VectorStaticAccessor },
                { nameof(Vector4.UnitZ), VectorStaticAccessor },
                { nameof(Vector4.UnitW), VectorStaticAccessor },
                { nameof(Vector4.Transform), Vector4Transform }
            };
            ret.Add("System.Numerics.Vector4", new DictionaryTypeInvocationTranslator(v4Mappings));

            Dictionary<string, InvocationTranslator> u2Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { ".ctor", VectorCtor },
            };
            ret.Add("ShaderGen.UInt2", new DictionaryTypeInvocationTranslator(u2Mappings));
            ret.Add("ShaderGen.Int2", new DictionaryTypeInvocationTranslator(u2Mappings));

            Dictionary<string, InvocationTranslator> m4x4Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { ".ctor", MatrixCtor }
            };
            ret.Add("System.Numerics.Matrix4x4", new DictionaryTypeInvocationTranslator(m4x4Mappings));

            Dictionary<string, InvocationTranslator> mathfMappings = new Dictionary<string, InvocationTranslator>()
            {
                // TODO Note cannot use nameof as MathF isn't included in this project...
                { "Abs", SimpleNameTranslator() },
                { "Acos", SimpleNameTranslator() },
                { "Acosh", Acosh },
                { "Asin", SimpleNameTranslator() },
                { "Asinh", Asinh },
                { "Atan", SimpleNameTranslator() },
                { "Atan2", SimpleNameTranslator() },
                { "Atanh", Atanh },
                { "Cbrt", CubeRoot }, // We can calculate the 1/3rd power, which might not give exactly the same result?
                { "Ceiling", SimpleNameTranslator("ceil") },
                { "Cos", SimpleNameTranslator() },
                { "Cosh", SimpleNameTranslator() },
                { "Exp", SimpleNameTranslator() },
                { "Floor", SimpleNameTranslator() },
                // TODO IEEERemainder(Single, Single) - see https://stackoverflow.com/questions/1971645/is-math-ieeeremainderx-y-equivalent-to-xy
                // How close is it to frac()?
                { "Log", Log },
                { "Log10", SimpleNameTranslator() },
                { "Max", SimpleNameTranslator() },
                { "Min", SimpleNameTranslator() },
                { "Pow", SimpleNameTranslator() },
                { "Round", Round },
                { "Sin", SimpleNameTranslator() },
                { "Sinh", SimpleNameTranslator() },
                { "Sqrt", SimpleNameTranslator() },
                { "Tan", SimpleNameTranslator() },
                { "Tanh", SimpleNameTranslator() },
                { "Truncate", SimpleNameTranslator() }
            };
            ret.Add("System.MathF", new DictionaryTypeInvocationTranslator(mathfMappings));

            ret.Add("ShaderGen.ShaderSwizzle", new SwizzleTranslator());

            Dictionary<string, InvocationTranslator> vectorExtensionMappings = new Dictionary<string, InvocationTranslator>()
            {
                { nameof(VectorExtensions.GetComponent), VectorGetComponent },
                { nameof(VectorExtensions.SetComponent), VectorSetComponent },
            };
            ret.Add("ShaderGen.VectorExtensions", new DictionaryTypeInvocationTranslator(vectorExtensionMappings));

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

        private static string VectorGetComponent(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"{parameters[0].Identifier}[{parameters[1].Identifier}]";
        }

        private static string VectorSetComponent(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"{parameters[0].Identifier}[{parameters[1].Identifier}] = {parameters[2].Identifier}";
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

        private static InvocationTranslator SimpleNameTranslator(string nameTarget = null)
        {
            return (type, method, parameters) =>
            {
                return $"{nameTarget ?? method.ToLower()}({InvocationParameterInfo.GetInvocationParameterList(parameters)})";
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

        private static string SampleComparisonLevelZero(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName == "ShaderGen.DepthTexture2DArrayResource")
            {
                return $"{parameters[0].Identifier}.SampleCmpLevelZero({parameters[1].Identifier}, float3({parameters[2].Identifier}, {parameters[3].Identifier}), {parameters[4].Identifier})";
            }
            else
            {
                return $"{parameters[0].Identifier}.SampleCmpLevelZero({parameters[1].Identifier}, {parameters[2].Identifier}, {parameters[3].Identifier})";
            }
        }

        private static string Load(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName == nameof(ShaderGen) + "." + nameof(Texture2DResource))
            {
                return $"{parameters[0].Identifier}.Load(int3({parameters[2].Identifier}, {parameters[3].Identifier}))";
            }
            else if (parameters[0].FullTypeName == nameof(ShaderGen) + "." + nameof(Texture2DMSResource))
            {
                return $"{parameters[0].Identifier}.Load({parameters[2].Identifier}, {parameters[3].Identifier})";
            }
            else if (parameters[0].FullTypeName.Contains("RWTexture2D"))
            {
                return $"{parameters[0].Identifier}[{parameters[1].Identifier}]";
            }
            else
            {
                throw new ShaderGenerationException($"Unable to process ShaderBuiltins.Load because type {parameters[0].FullTypeName} was not recognized.");
            }
        }

        private static string Store(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"{parameters[0].Identifier}[{parameters[1].Identifier}] = {parameters[2].Identifier}";
        }


        private static string Discard(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"discard";
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

        private static string InterlockedAdd(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"_SG_Util_InterlockedAdd({parameters[0].Identifier}, {parameters[1].Identifier}, {parameters[2].Identifier})";
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
            else if (name == "ShaderGen.Int2") { shaderType = "int2"; elementCount = 2; }
            else if (name == "ShaderGen.UInt2") { shaderType = "uint2"; elementCount = 2; }
            else { throw new ShaderGenerationException("VectorCtor translator was called on an invalid type: " + name); }
        }

        /// <summary>
        /// Creates a Vector Constant (or float constant), where all values set to the specified value.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private static string Cvc(string typeName, float value)
        {
            string v = value.ToString("0.0###########");
            if (typeName == "System.Single" || typeName == "float") // TODO Why are we getting float?
            {
                return v;
            }

            GetVectorTypeInfo(typeName, out string shaderType, out int elementCount);
            return $"{shaderType}({string.Join(",", Enumerable.Range(0, elementCount).Select(i => v))})";
        }

        private static string CubeRoot(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            InvocationParameterInfo firstParameter = parameters[0];
            return $"pow({firstParameter.Identifier}, {Cvc(firstParameter.FullTypeName, 0.333333333333333f)})";
        }

        private static string Log(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters.Length < 2)
            {
                return $"log({parameters[0].Identifier})";
            }

            // TODO Get computed constant value for parameter 2 rather than simple string
            string param2 = parameters[1].Identifier;
            if (float.TryParse(param2, out float @base))
            {
                if (Math.Abs(@base - 2f) < float.Epsilon)
                {
                    return $"log2({parameters[0].Identifier})";
                }

                if (Math.Abs(@base - Math.E) < float.Epsilon)
                {
                    return $"log({parameters[0].Identifier})";
                }
            }

            return $"(log({parameters[0].Identifier})/log({parameters[1].Identifier}))";
        }

        private static string Round(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            // TODO Should we use RoundEven here for safety??
            if (parameters.Length < 2)
            {
                return $"round({parameters[0].Identifier})";
            }

            // TODO Need to Implement to support MathF fully
            // Round(Single, Int32)
            // Round(Single, Int32, MidpointRounding)
            // Round(Single, MidpointRounding)
            throw new NotImplementedException();
        }

        private static string Acosh(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            InvocationParameterInfo firstParameter = parameters[0];
            string target = firstParameter.Identifier;
            return $"log({target} + sqrt({target}*{target}-{Cvc(firstParameter.FullTypeName, 1f)}))";
        }

        private static string Asinh(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            InvocationParameterInfo firstParameter = parameters[0];
            string target = firstParameter.Identifier;
            return $"log({target} + sqrt({target}*{target}+{Cvc(firstParameter.FullTypeName, 1f)}))";
        }

        private static string Atan(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return
                $"{(parameters.Length < 2 ? "atan" : "atan2")}({InvocationParameterInfo.GetInvocationParameterList(parameters)})";
        }

        private static string Atanh(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            InvocationParameterInfo firstParameter = parameters[0];
            string target = firstParameter.Identifier;
            // Note this is pretty inaccurate on the GPU!
            return
                $"log(({Cvc(firstParameter.FullTypeName, 1f)}+{target})/({Cvc(firstParameter.FullTypeName, 1f)}-{target})/{Cvc(firstParameter.FullTypeName, 2f)})";
        }
    }

    public delegate string InvocationTranslator(string typeName, string methodName, InvocationParameterInfo[] parameters);
}
