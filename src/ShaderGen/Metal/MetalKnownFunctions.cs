using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
                {nameof(ShaderBuiltins.Abs), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Acos), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Acosh), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Asin), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Asinh), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Atan), Atan},
                {nameof(ShaderBuiltins.Atanh), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Cbrt), CubeRoot},
                {nameof(ShaderBuiltins.Ceiling), SimpleNameTranslator("ceil")},
                {nameof(ShaderBuiltins.Clamp), Clamp},
                {nameof(ShaderBuiltins.ClipToTextureCoordinates), ClipToTextureCoordinates},
                {nameof(ShaderBuiltins.Cos), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Cosh), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Ddx), SimpleNameTranslator("dfdx")},
                {nameof(ShaderBuiltins.DdxFine), SimpleNameTranslator("dfdx")},
                {nameof(ShaderBuiltins.Ddy), SimpleNameTranslator("dfdy")},
                {nameof(ShaderBuiltins.DdyFine), SimpleNameTranslator("dfdy")},
                {nameof(ShaderBuiltins.Degrees), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Discard), Discard},
                {nameof(ShaderBuiltins.DispatchThreadID), DispatchThreadID},
                {nameof(ShaderBuiltins.Exp), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Exp2), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Floor), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.FMod), FMod},
                {nameof(ShaderBuiltins.Frac), SimpleNameTranslator("fract")},
                {nameof(ShaderBuiltins.GroupThreadID), GroupThreadID},
                {nameof(ShaderBuiltins.InstanceID), InstanceID},
                {nameof(ShaderBuiltins.InterlockedAdd), InterlockedAdd},
                {nameof(ShaderBuiltins.IsFrontFace), IsFrontFace},
                {nameof(ShaderBuiltins.Lerp), SimpleNameTranslator("mix")},
                {nameof(ShaderBuiltins.Load), Load},
                {nameof(ShaderBuiltins.Log), Log},
                {nameof(ShaderBuiltins.Log2), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Log10), Log10},
                {nameof(ShaderBuiltins.Max), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Min), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Mod), Mod},
                {nameof(ShaderBuiltins.Mul), MatrixMul},
                {nameof(ShaderBuiltins.Pow), Pow},
                {nameof(ShaderBuiltins.Radians), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Round), Round},
                {nameof(ShaderBuiltins.Rsqrt), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Sample), Sample},
                {nameof(ShaderBuiltins.SampleComparisonLevelZero), SampleComparisonLevelZero},
                {nameof(ShaderBuiltins.SampleGrad), SampleGrad},
                {nameof(ShaderBuiltins.Saturate), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Sin), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Sinh), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.SmoothStep), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Sqrt), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Store), Store},
                {nameof(ShaderBuiltins.Tan), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Tanh), SimpleNameTranslator()},
                {nameof(ShaderBuiltins.Truncate), SimpleNameTranslator("trunc")},
                {nameof(ShaderBuiltins.VertexID), VertexID}
            };
            ret.Add("ShaderGen.ShaderBuiltins", new DictionaryTypeInvocationTranslator(builtinMappings));

            Dictionary<string, InvocationTranslator> v2Mappings = new Dictionary<string, InvocationTranslator>()
            {
                { ".ctor", VectorCtor },
                { nameof(Vector2.Abs), SimpleNameTranslator() },
                { nameof(Vector2.Add), BinaryOpTranslator("+") },
                { nameof(Vector2.Clamp), SimpleNameTranslator() },
                // Doesn't exist! { nameof(Vector2.Cos), SimpleNameTranslator("cos") },
                { nameof(Vector2.Distance), SimpleNameTranslator("distance") },
                { nameof(Vector2.DistanceSquared), DistanceSquared },
                { nameof(Vector2.Divide), BinaryOpTranslator("/") },
                { nameof(Vector2.Dot), SimpleNameTranslator() },
                { nameof(Vector2.Lerp), SimpleNameTranslator("mix") },
                { nameof(Vector2.Max), SimpleNameTranslator() },
                { nameof(Vector2.Min), SimpleNameTranslator() },
                { nameof(Vector2.Multiply), BinaryOpTranslator("*") },
                { nameof(Vector2.Negate), Negate },
                { nameof(Vector2.Normalize), SimpleNameTranslator() },
                { nameof(Vector2.Reflect), SimpleNameTranslator() },
                // Doesn't exist! { nameof(Vector2.Sin), SimpleNameTranslator("sin") },
                { nameof(Vector2.SquareRoot), SimpleNameTranslator("sqrt") },
                { nameof(Vector2.Subtract), BinaryOpTranslator("-") },
                { nameof(Vector2.Length), SimpleNameTranslator() },
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
                { nameof(Vector3.Abs), SimpleNameTranslator() },
                { nameof(Vector3.Add), BinaryOpTranslator("+") },
                { nameof(Vector3.Clamp), SimpleNameTranslator() },
                // Doesn't exist! { nameof(Vector3.Cos), SimpleNameTranslator("cos") },
                { nameof(Vector3.Cross), SimpleNameTranslator() },
                { nameof(Vector3.Distance), SimpleNameTranslator() },
                { nameof(Vector3.DistanceSquared), DistanceSquared },
                { nameof(Vector3.Divide), BinaryOpTranslator("/") },
                { nameof(Vector3.Dot), SimpleNameTranslator() },
                { nameof(Vector3.Lerp), SimpleNameTranslator("mix") },
                { nameof(Vector3.Max), SimpleNameTranslator() },
                { nameof(Vector3.Min), SimpleNameTranslator() },
                { nameof(Vector3.Multiply), BinaryOpTranslator("*") },
                { nameof(Vector3.Negate), Negate },
                { nameof(Vector3.Normalize), SimpleNameTranslator() },
                { nameof(Vector3.Reflect), SimpleNameTranslator() },
                // Doesn't exist! { nameof(Vector3.Sin), SimpleNameTranslator("sin") },
                { nameof(Vector3.SquareRoot), SimpleNameTranslator("sqrt") },
                { nameof(Vector3.Subtract), BinaryOpTranslator("-") },
                { nameof(Vector3.Length), SimpleNameTranslator() },
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
                { nameof(Vector4.Abs), SimpleNameTranslator() },
                { nameof(Vector4.Add), BinaryOpTranslator("+") },
                { nameof(Vector4.Clamp), SimpleNameTranslator() },
                // Doesn't exist! { nameof(Vector4.Cos), SimpleNameTranslator("cos") },
                { nameof(Vector4.Distance), SimpleNameTranslator() },
                { nameof(Vector4.DistanceSquared), DistanceSquared },
                { nameof(Vector4.Divide), BinaryOpTranslator("/") },
                { nameof(Vector4.Dot), SimpleNameTranslator() },
                { nameof(Vector4.Lerp), SimpleNameTranslator("mix") },
                { nameof(Vector4.Max), SimpleNameTranslator() },
                { nameof(Vector4.Min), SimpleNameTranslator() },
                { nameof(Vector4.Multiply), BinaryOpTranslator("*") },
                { nameof(Vector4.Negate), Negate },
                { nameof(Vector4.Normalize), SimpleNameTranslator() },
                // Doesn't exist! { nameof(Vector4.Reflect), SimpleNameTranslator("reflect") },
                // Doesn't exist! { nameof(Vector4.Sin), SimpleNameTranslator("sin") },
                { nameof(Vector4.SquareRoot), SimpleNameTranslator("sqrt") },
                { nameof(Vector4.Subtract), BinaryOpTranslator("-") },
                { nameof(Vector4.Length), SimpleNameTranslator() },
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
                {".ctor", VectorCtor},
            };
            ret.Add("ShaderGen.UInt2", new DictionaryTypeInvocationTranslator(u2Mappings));
            ret.Add("ShaderGen.Int2", new DictionaryTypeInvocationTranslator(u2Mappings));

            Dictionary<string, InvocationTranslator> m4x4Mappings = new Dictionary<string, InvocationTranslator>()
            {
                {".ctor", MatrixCtor}
            };
            ret.Add("System.Numerics.Matrix4x4", new DictionaryTypeInvocationTranslator(m4x4Mappings));

            Dictionary<string, InvocationTranslator> mathfMappings = new Dictionary<string, InvocationTranslator>()
            {
                // TODO Note cannot use nameof as MathF isn't included in this project...
                { "Abs", SimpleNameTranslator() },
                { "Acos", SimpleNameTranslator() },
                { "Acosh", SimpleNameTranslator() },
                { "Asin", SimpleNameTranslator() },
                { "Asinh", SimpleNameTranslator() },
                { "Atan", Atan },
                { "Atan2", Atan }, // Note atan supports both (x) and (y,x)
                { "Atanh", SimpleNameTranslator() },
                { "Cbrt", CubeRoot }, // We can calculate the 1/3rd power, which might not give exactly the same result?
                { "Ceiling", SimpleNameTranslator("ceil") },
                { "Cos", SimpleNameTranslator() },
                { "Cosh", SimpleNameTranslator() },
                { "Exp", SimpleNameTranslator() },
                { "Floor", SimpleNameTranslator() },
                // TODO IEEERemainder(Single, Single) - see https://stackoverflow.com/questions/1971645/is-math-ieeeremainderx-y-equivalent-to-xy
                // How close is it to frac()?
                { "Log", Log },
                { "Log10", Log10 },
                { "Max", SimpleNameTranslator() },
                { "Min", SimpleNameTranslator() },
                { "Pow", Pow },
                { "Round", Round },
                { "Sin", SimpleNameTranslator() },
                { "Sinh", SimpleNameTranslator() },
                { "Sqrt", SimpleNameTranslator() },
                { "Tan", SimpleNameTranslator() },
                { "Tanh", SimpleNameTranslator() },
                { "Truncate", SimpleNameTranslator() }
            };
            ret.Add("System.MathF", new DictionaryTypeInvocationTranslator(mathfMappings));

            ret.Add("ShaderGen.ShaderSwizzle", new MetalSwizzleTranslator());

            Dictionary<string, InvocationTranslator> vectorExtensionMappings =
                new Dictionary<string, InvocationTranslator>()
                {
                    {nameof(VectorExtensions.GetComponent), VectorGetComponent},
                    {nameof(VectorExtensions.SetComponent), VectorSetComponent},
                };
            ret.Add("ShaderGen.VectorExtensions", new DictionaryTypeInvocationTranslator(vectorExtensionMappings));

            return ret;
        }

        private static string Clamp(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName == "float")
            {
                return
                    $"clamp({parameters[0].Identifier}, (float){parameters[1].Identifier}, (float){parameters[2].Identifier})";
            }
            else
            {
                return SimpleNameTranslator("clamp")(typeName, methodName, parameters);
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
            return
                $"dot({parameters[0].Identifier} - {parameters[1].Identifier}, {parameters[0].Identifier} - {parameters[1].Identifier})";
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

        private static string VectorGetComponent(string typeName, string methodName,
            InvocationParameterInfo[] parameters)
        {
            return $"{parameters[0].Identifier}[{parameters[1].Identifier}]";
        }

        private static string VectorSetComponent(string typeName, string methodName,
            InvocationParameterInfo[] parameters)
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
                // Metal texture array sample function:
                // sample(sampler s, float2 coord, uint array)
                return
                    $"{parameters[0].Identifier}.sample({parameters[1].Identifier}, {parameters[2].Identifier}, {parameters[3].Identifier})";
            }
            else
            {
                return $"{parameters[0].Identifier}.sample({parameters[1].Identifier}, {parameters[2].Identifier})";
            }
        }

        private static string SampleGrad(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName == "ShaderGen.Texture2DArrayResource")
            {
                return
                    $"{parameters[0].Identifier}.sample({parameters[1].Identifier}, {parameters[2].Identifier}, {parameters[3].Identifier}, gradient2d({parameters[4].Identifier}, {parameters[5].Identifier}))";
            }
            else
            {
                return
                    $"{parameters[0].Identifier}.sample({parameters[1].Identifier}, {parameters[2].Identifier}, gradient2d({parameters[3].Identifier}, {parameters[4].Identifier}))";
            }
        }

        private static string SampleComparisonLevelZero(string typeName, string methodName,
            InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName == "ShaderGen.DepthTexture2DArrayResource")
            {
                // Metal texture array sample_compare function:
                // sample_compare(sampler s, float2 coord, uint array, float compare_value)
                return
                    $"{parameters[0].Identifier}.sample_compare({parameters[1].Identifier}, {parameters[2].Identifier}, {parameters[3].Identifier}, {parameters[4].Identifier})";
            }
            else
            {
                return
                    $"{parameters[0].Identifier}.sample_compare({parameters[1].Identifier}, {parameters[2].Identifier}, {parameters[3].Identifier})";
            }
        }

        private static string Load(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName.Contains("RWTexture2D"))
            {
                if (parameters[0].FullTypeName.Contains("<float>"))
                {
                    return $"{parameters[0].Identifier}.read({parameters[1].Identifier}).r";
                }
                else
                {
                    return $"{parameters[0].Identifier}.read({parameters[1].Identifier})";
                }
            }
            else
            {
                return
                    $"{parameters[0].Identifier}.read(uint2({parameters[2].Identifier}.x, {parameters[2].Identifier}.y), {parameters[3].Identifier})";
            }
        }

        private static string Store(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters[0].FullTypeName.Contains("<float>"))
            {
                return
                    $"{parameters[0].Identifier}.write(float4({parameters[2].Identifier}), {parameters[1].Identifier})";
            }
            else
            {
                return $"{parameters[0].Identifier}.write({parameters[2].Identifier}, {parameters[1].Identifier})";
            }
        }

        private static string Discard(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return $"discard_fragment()";
        }

        private static string ClipToTextureCoordinates(string typeName, string methodName,
            InvocationParameterInfo[] parameters)
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

        private static string InterlockedAdd(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            return
                $"atomic_fetch_add_explicit(&{parameters[0].Identifier}[{parameters[1].Identifier}], {parameters[2].Identifier}, memory_order_relaxed)";
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

        private static string VectorStaticAccessor(string typeName, string methodName,
            InvocationParameterInfo[] parameters)
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
                if (elementCount == 2)
                {
                    paramList = "1, 0";
                }
                else if (elementCount == 3)
                {
                    paramList = "1, 0, 0";
                }
                else
                {
                    paramList = "1, 0, 0, 0";
                }

                return $"{shaderType}({paramList})";
            }
            else if (methodName == "UnitY")
            {
                string paramList;
                if (elementCount == 2)
                {
                    paramList = "0, 1";
                }
                else if (elementCount == 3)
                {
                    paramList = "0, 1, 0";
                }
                else
                {
                    paramList = "0, 1, 0, 0";
                }

                return $"{shaderType}({paramList})";
            }
            else if (methodName == "UnitZ")
            {
                string paramList;
                if (elementCount == 3)
                {
                    paramList = "0, 0, 1";
                }
                else
                {
                    paramList = "0, 0, 1, 0";
                }

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
            if (name == "System.Numerics.Vector2")
            {
                shaderType = "float2";
                elementCount = 2;
            }
            else if (name == "System.Numerics.Vector3")
            {
                shaderType = "float3";
                elementCount = 3;
            }
            else if (name == "System.Numerics.Vector4")
            {
                shaderType = "float4";
                elementCount = 4;
            }
            else if (name == "ShaderGen.Int2")
            {
                shaderType = "int2";
                elementCount = 2;
            }
            else if (name == "ShaderGen.UInt2")
            {
                shaderType = "uint2";
                elementCount = 2;
            }
            else
            {
                throw new ShaderGenerationException("VectorCtor translator was called on an invalid type: " + name);
            }
        }

        private static string CubeRoot(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            string pType = parameters[0].FullTypeName;
            if (pType == "System.Single" || pType == "float") // TODO Why are we getting float?
            {
                return $"pow({parameters[0].Identifier}, 0.333333333333333)";
            }

            GetVectorTypeInfo(pType, out string shaderType, out int elementCount);
            return
                $"pow({parameters[0].Identifier}, {shaderType}({string.Join(",", Enumerable.Range(0, elementCount).Select(i => "0.333333333333333"))}))";
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

        private static string Log10(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            // Divide by Log(10) = 2.30258509299405 as OpenGL doesn't suppport log10 natively
            return $"(log({parameters[0].Identifier})/2.30258509299405)";
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

        private static string Atan(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters.Length < 2)
            {
                return $"atan({InvocationParameterInfo.GetInvocationParameterList(parameters)})";
            }

            return parameters[0].FullTypeName.Contains("Vector")
                ? $"atan2({InvocationParameterInfo.GetInvocationParameterList(parameters)})"
                : $"atan2({parameters[0].Identifier}, (float){parameters[1].Identifier})";
        }

        private static string Pow(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            if (parameters.Length < 2)
            {
                return $"pow({InvocationParameterInfo.GetInvocationParameterList(parameters)})";
            }

            return parameters[0].FullTypeName.Contains("Vector")
                ? $"pow({InvocationParameterInfo.GetInvocationParameterList(parameters)})"
                : $"pow({parameters[0].Identifier}, (float){parameters[1].Identifier})";
        }

        private static string FMod(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            // D3D & Vulkan return Max when max < min, but OpenGL returns Min, so we need
            // to correct by returning Max when max < min.
            bool isFloat = parameters[1].FullTypeName == "System.Single" || parameters[1].FullTypeName == "float";
            string p0 = $"{parameters[0].Identifier}`";
            string p1 = $"{parameters[1].Identifier}{(isFloat ? string.Empty : "`")}";
            return AddCheck(parameters[0].FullTypeName,
                $"({p0}-{p1}*trunc({p0}/{p1}))");
        }

        private static string Mod(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            // D3D & Vulkan return Max when max < min, but OpenGL returns Min, so we need
            // to correct by returning Max when max < min.
            bool isFloat = parameters[1].FullTypeName == "System.Single" || parameters[1].FullTypeName == "float";
            string p0 = $"{parameters[0].Identifier}`";
            string p1 = $"{parameters[1].Identifier}{(isFloat ? string.Empty : "`")}";
            return AddCheck(parameters[0].FullTypeName,
                $"({p0}-{p1}*floor({p0}/{p1}))");
        }

        private static readonly HashSet<string> _oneDimensionalTypes =
            new HashSet<string>(new[]
                {
                    "System.Single",
                    "float",
                    "System.Int32",
                    "int",
                    "System.UInt32",
                    "uint"
                },
                StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Implements a check for each element of a vector.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="check">The check.</param>
        /// <returns></returns>
        private static string AddCheck(string typeName, string check)
        {
            if (_oneDimensionalTypes.Contains(typeName))
            {
                // The check can stay as it is, strip the '`' characters.
                return check.Replace("`", string.Empty);
            }

            GetVectorTypeInfo(typeName, out string shaderType, out int elementCount);
            return
                $"{shaderType}({string.Join(",", Enumerable.Range(0, elementCount).Select(a => check.Replace("`", $"[{a}]")))})";
        }
    }
}
