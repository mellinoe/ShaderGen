using System.Linq;

namespace ShaderGen
{
    public class ShaderModel
    {
        public StructureDefinition[] Structures { get; }
        public UniformDefinition[] Uniforms { get; }
        public ShaderFunction EntryFunction { get; }

        public ShaderModel(StructureDefinition[] structures, UniformDefinition[] uniforms, ShaderFunction entryFunction)
        {
            Structures = structures;
            Uniforms = uniforms;
            EntryFunction = entryFunction;
        }

        public StructureDefinition GetStructureDefinition(TypeReference typeRef) => GetStructureDefinition(typeRef.Name);
        public StructureDefinition GetStructureDefinition(string typeName)
        {
            return Structures.FirstOrDefault(sd => sd.Name == typeName);
        }
    }
}
