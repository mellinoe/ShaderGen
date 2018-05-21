using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ShaderGen.Glsl
{
    public class GlslEs300Backend : GlslBackendBase
    {
        public GlslEs300Backend(Compilation compilation) : base(compilation)
        {
        }

        protected override string CSharpToShaderTypeCore(string fullType)
        {
            return GlslKnownTypes.GetMappedName(fullType, false)
                .Replace(".", "_")
                .Replace("+", "_");
        }

        protected override void WriteVersionHeader(
            ShaderFunction function,
            ShaderFunctionAndMethodDeclarationSyntax[] orderedFunctions,
            StringBuilder sb)
        {
            bool anyUsesStructuredBuffer = function.UsesStructuredBuffer
                || orderedFunctions.Any(i => i.Function.UsesStructuredBuffer);

            bool useVersion320 = function.UsesTexture2DMS;
            bool useVersion310 = anyUsesStructuredBuffer;
            string versionNumber = useVersion320 ? "320" :
                                   useVersion310 ? "310" : "300";
            string version = $"{versionNumber} es";
            sb.AppendLine($"#version {version}");
            sb.AppendLine($"precision mediump float;");
            sb.AppendLine($"precision mediump int;");
            sb.AppendLine($"precision mediump sampler2D;");
            sb.AppendLine($"precision mediump sampler2DShadow;");
            sb.AppendLine($"precision mediump sampler2DArray;");
            sb.AppendLine($"precision mediump sampler2DArrayShadow;");
            if (useVersion320)
            {
                sb.AppendLine($"precision mediump sampler2DMS;");
            }
            if (function.UsesRWTexture2D)
            {
                sb.AppendLine($"precision mediump image2D;");
            }
            sb.AppendLine();

            sb.AppendLine($"struct SamplerDummy {{ int _dummyValue; }};");
            sb.AppendLine($"struct SamplerComparisonDummy {{ int _dummyValue; }};");
            sb.AppendLine();
        }

        protected override void WriteSampler(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"SamplerDummy {CorrectIdentifier(rd.Name)} = SamplerDummy(0);");
            sb.AppendLine();
        }

        protected override void WriteSamplerComparison(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"SamplerComparisonDummy {CorrectIdentifier(rd.Name)} = SamplerComparisonDummy(0);");
            sb.AppendLine();
        }

        protected override void WriteTexture2D(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"uniform sampler2D {CorrectIdentifier(rd.Name)};");
            sb.AppendLine();
        }

        protected override void WriteTexture2DArray(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"uniform sampler2DArray {CorrectIdentifier(rd.Name)};");
            sb.AppendLine();
        }

        protected override void WriteTextureCube(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"uniform samplerCube {CorrectIdentifier(rd.Name)};");
            sb.AppendLine();
        }

        protected override void WriteTexture2DMS(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"uniform sampler2DMS {CorrectIdentifier(rd.Name)};");
            sb.AppendLine();
        }

        protected override void WriteDepthTexture2D(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"uniform sampler2DShadow {CorrectIdentifier(rd.Name)};");
            sb.AppendLine();
        }

        protected override void WriteDepthTexture2DArray(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"uniform sampler2DArrayShadow {CorrectIdentifier(rd.Name)};");
            sb.AppendLine();
        }

        protected override void WriteUniform(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"layout(std140) uniform {rd.Name}");
            sb.AppendLine("{");
            sb.AppendLine($"    {CSharpToShaderType(rd.ValueType.Name)} field_{CorrectIdentifier(rd.Name.Trim())};");
            sb.AppendLine("};");
            sb.AppendLine();
        }

        protected override void WriteStructuredBuffer(StringBuilder sb, ResourceDefinition rd, bool isReadOnly, int index)
        {
            string valueType = rd.ValueType.Name;
            string type = valueType == "ShaderGen.AtomicBufferUInt32"
                ? "uint"
                : valueType == "ShaderGen.AtomicBufferInt32"
                    ? "int"
                    : CSharpToShaderType(rd.ValueType.Name);
            string readOnlyStr = isReadOnly ? " readonly" : " ";
            sb.AppendLine($"layout(std430, binding = {index}){readOnlyStr} buffer {rd.Name}");
            sb.AppendLine("{");
            sb.AppendLine($"    {type} field_{CorrectIdentifier(rd.Name.Trim())}[];");
            sb.AppendLine("};");
        }

        protected override void WriteRWTexture2D(StringBuilder sb, ResourceDefinition rd, int index)
        {
            string layoutType;
            switch (rd.ValueType.Name)
            {
                case "System.Numerics.Vector4":
                    layoutType = "rgba32f";
                    break;
                case "System.Single":
                    layoutType = "r32f";
                    break;
                default: throw new ShaderGenerationException($"Invalid RWTexture2D type. T must be Vector4 or float.");
            }
            sb.AppendLine($"layout({layoutType}, binding = {index}) uniform image2D {CorrectIdentifier(rd.Name)};");
            sb.AppendLine();
        }

        protected override string FormatInvocationCore(string setName, string type, string method, InvocationParameterInfo[] parameterInfos)
        {
            return GlslEs300KnownFunctions.TranslateInvocation(type, method, parameterInfos);
        }

        internal override string CorrectBinaryExpression(
            string leftExpr,
            string leftExprType,
            string operatorToken,
            string rightExpr,
            string rightExprType)
        {
            if (IsIntegerType(leftExprType) && !IsIntegerType(rightExprType))
            {
                leftExpr = $"float({leftExpr})";
            }
            else if (IsIntegerType(rightExprType) && !IsIntegerType(leftExprType))
            {
                rightExpr = $"float({rightExpr})";
            }

            return $"{leftExpr} {operatorToken} {rightExpr}";
        }

        private bool IsIntegerType(string exprType)
        {
            return exprType == "System.Int32" || exprType == "System.UInt32"
                || exprType == "int" || exprType == "uint";
        }

        internal override string CorrectAssignedValue(string leftExprType, string value, string valueType)
        {
            if (valueType == "System.Int32" && leftExprType != "System.Int32")
            {
                value = $"float({value})";
            }

            return $"{value}";
        }

        protected override string FormatInvocationParameter(ParameterDefinition def, InvocationParameterInfo ipi)
        {
            if (def.Type.Name == "System.Single" && IsIntegerType(ipi.FullTypeName))
            {
                return $"float({ipi.Identifier})";
            }
            else
            {
                return ipi.Identifier;
            }
        }

        protected override void WriteInOutVariable(
            StringBuilder sb,
            bool isInVar,
            bool isVertexStage,
            string normalizedType,
            string normalizedIdentifier,
            int index)
        {
            string qualifier = isInVar ? "in" : "out";
            string identifier;
            if ((isVertexStage && isInVar) || (!isVertexStage && !isInVar))
            {
                identifier = normalizedIdentifier;
            }
            else
            {
                Debug.Assert(isVertexStage || isInVar);
                identifier = $"fsin_{index}";
            }

            sb.AppendLine($"{qualifier} {normalizedType} {identifier};");
        }

        protected override void EmitGlPositionCorrection(StringBuilder sb)
        {
            sb.AppendLine($"        gl_Position.z = gl_Position.z * 2.0 - gl_Position.w;");
        }
    }
}
