namespace ShaderGen {
    public interface ILanguageBackend {
        void InitContext(string setName);
        void AddStructure(string setName, StructureDefinition sd);
    }
}