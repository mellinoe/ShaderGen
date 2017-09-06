namespace ShaderGen
{
    public class StructDefinition
    {
        public string Name { get; }
        public FieldDefinition[] Fields { get; }

        public StructDefinition(string name, FieldDefinition[] fields)
        {
            Name = name;
            Fields = fields;
        }
    }
}
