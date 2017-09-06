namespace ShaderGen
{
    public class FieldDefinition
    {
        public string Name { get; }
        public TypeReference Type { get; }

        public FieldDefinition(string name, TypeReference type)
        {
            Name = name;
            Type = type;
        }
    }
}
