using System.Linq;

namespace ShaderGen
{
    public class StructureDefinition
    {
        public string Name { get; }
        public FieldDefinition[] Fields { get; }
        public AlignmentInfo Alignment { get; }
        public bool CSharpMatchesShaderAlignment { get; }

        public StructureDefinition(string name, FieldDefinition[] fields, AlignmentInfo size)
        {
            Name = name;
            Fields = fields;
            Alignment = size;
            CSharpMatchesShaderAlignment = GetCSharpMatchesShaderAlignment();
        }

        private bool GetCSharpMatchesShaderAlignment()
        {
            int csharpOffset = 0;
            int shaderOffset = 0;
            for (int i = 0; i < Fields.Length; i++)
            {
                if (!CheckAlignments(Fields[i], ref csharpOffset, ref shaderOffset))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckAlignments(FieldDefinition fd, ref int cs, ref int shader)
        {
            if ((cs % fd.Alignment.CSharpAlignment) != 0 || (shader % fd.Alignment.ShaderAlignment) != 0)
            {
                return false;
            }

            cs += fd.Alignment.CSharpSize;
            shader += fd.Alignment.CSharpSize;
            return cs == shader;
        }
    }
}
