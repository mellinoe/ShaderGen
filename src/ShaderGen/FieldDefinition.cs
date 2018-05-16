namespace ShaderGen
{
    public class FieldDefinition
    {
        public string Name { get; }
        public TypeReference Type { get; }
        public SemanticType SemanticType { get; }
        /// <summary>
        /// The number of elements in an array, if this is an array field.
        /// Returns 0 if the field is not an array.
        /// </summary>
        public int ArrayElementCount { get; }
        public bool IsArray => ArrayElementCount > 0;
        public AlignmentInfo Alignment { get; }

        public FieldDefinition(
            string name,
            TypeReference type,
            SemanticType semanticType,
            int arrayElementCount,
            AlignmentInfo size)
        {
            Name = name;
            Type = type;
            SemanticType = semanticType;
            ArrayElementCount = arrayElementCount;
            Alignment = size;
        }
    }
}
