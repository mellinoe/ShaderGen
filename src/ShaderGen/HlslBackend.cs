using Microsoft.CodeAnalysis;
using System.Text;

namespace ShaderGen
{
    public class HlslBackend : LanguageBackend
    {
        public HlslBackend(SemanticModel model) : base(model)
        {
        }

        protected override string CSharpToShaderTypeCore(string fullType)
        {
            return HlslKnownTypes.GetMappedName(fullType)
                .Replace(".", "_");
        }

        protected override void WriteStructure(StringBuilder sb, StructDefinition sd)
        {
            sb.AppendLine($"struct {CSharpToShaderType(sd.Name)}");
            sb.AppendLine("{");
            foreach (FieldDefinition field in sd.Fields)
            {
                sb.AppendLine($"    {CSharpToShaderType(field.Type.Name.Trim())} {field.Name.Trim()};");
            }
            sb.AppendLine("};");
            sb.AppendLine();
        }

        protected override void WriteUniform(StringBuilder sb, UniformDefinition ud)
        {
            sb.AppendLine($"cbuffer {ud.Name}Buffer : register(b{ud.Binding})");
            sb.AppendLine("{");
            sb.AppendLine($"    {HlslKnownTypes.GetMappedName(ud.Type.Name.Trim())} {ud.Name.Trim()};");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        protected override string CSharpToShaderFunctionNameCore(string type, string method)
        {
            return HlslKnownFunctions.GetMappedFunctionName(type, method);
        }
    }
}
