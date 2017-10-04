namespace ShaderGen
{
    public class TypeReference
    {
        public string Name { get; }

        public TypeReference(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }
}
