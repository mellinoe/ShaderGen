namespace ShaderGen
{
    public class ShaderModel
    {
        public StructDefinition[] Structures { get; }
        public UniformDefinition[] Uniforms { get; }
        public ShaderFunction EntryFunction { get; }

        public ShaderModel(StructDefinition[] structures, UniformDefinition[] uniforms, ShaderFunction entryFunction)
        {
            Structures = structures;
            Uniforms = uniforms;
            EntryFunction = entryFunction;
        }
    }
}
