namespace ShaderGen
{
    public class FieldDefinition
    {
        public string Name { get; }
        public TypeReference Type { get; }
        public SemanticType SemanticType { get; }

        public FieldDefinition(string name, TypeReference type, SemanticType semanticType)
        {
            Name = name;
            Type = type;
            SemanticType = semanticType;
        }
    }
}
