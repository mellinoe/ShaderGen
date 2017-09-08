using CodeGeneration.Roslyn;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShaderGen
{
    public class HlslWriter
    {
        private readonly List<StructDefinition> _structs;
        private readonly List<UniformDefinition> _uniforms;
        private readonly HlslTypeTranslator _typeTranslator;

        public HlslWriter(List<StructDefinition> structs, List<UniformDefinition> uniforms, TransformationContext context)
        {
            _structs = structs;
            _uniforms = uniforms;
            _typeTranslator = new HlslTypeTranslator(context);
        }

        public string GetHlslText()
        {
            StringBuilder sb = new StringBuilder();
            foreach (UniformDefinition ud in _uniforms)
            {
                sb.AppendLine($"cbuffer {ud.Name}Buffer : register(b{ud.Binding})");
                sb.AppendLine("{");
                sb.AppendLine($"    {HlslKnownTypes.GetMappedName(ud.Type.Name.Trim())} {ud.Name.Trim()};");
                sb.AppendLine("}");
                sb.AppendLine();
            }

            foreach (StructDefinition sd in _structs)
            {
                sb.AppendLine($"struct {_typeTranslator.CSharpToShaderType(sd.Name)}");
                sb.AppendLine("{");
                foreach (FieldDefinition field in sd.Fields)
                {
                    sb.AppendLine($"    {_typeTranslator.CSharpToShaderType(field.Type.Name.Trim())} {field.Name.Trim()};");
                }
                sb.AppendLine("};");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
