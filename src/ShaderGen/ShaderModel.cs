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
    }
}
