using System.Text;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace ShaderGen
{
    public class Glsl330Backend : GlslBackendBase
    {
        public Glsl330Backend(Compilation compilation) : base(compilation)
        {
        }

        protected override void WriteVersionHeader(StringBuilder sb)
        {
            sb.AppendLine("#version 330 core");
            sb.AppendLine();
        }

        protected override void WriteSampler(StringBuilder sb, ResourceDefinition rd)
        {
        }

        protected override void WriteTexture2D(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"uniform sampler2D {CorrectIdentifier(rd.Name)};");
            sb.AppendLine();
        }

        protected override void WriteTextureCube(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"uniform samplerCube {CorrectIdentifier(rd.Name)};");
            sb.AppendLine();
        }

        protected override void WriteUniform(StringBuilder sb, ResourceDefinition rd)
        {
            sb.AppendLine($"uniform {rd.Name}Buffer");
            sb.AppendLine("{");
            sb.AppendLine($"    {CSharpToShaderType(rd.ValueType.Name)} {CorrectIdentifier(rd.Name.Trim())};");
            sb.AppendLine("};");
            sb.AppendLine();
        }

        protected override string FormatInvocationCore(string type, string method, InvocationParameterInfo[] parameterInfos)
        {
            return Glsl330KnownFunctions.TranslateInvocation(type, method, parameterInfos);
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
    }
}
