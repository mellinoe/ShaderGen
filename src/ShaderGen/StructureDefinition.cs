namespace ShaderGen
{
    public class StructureDefinition
    {
        public string Name { get; }
        public FieldDefinition[] Fields { get; }

        public StructureDefinition(string name, FieldDefinition[] fields)
        {
            Name = name;
            Fields = fields;
        }
    }
}
