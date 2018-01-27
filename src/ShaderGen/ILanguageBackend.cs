namespace ShaderGen {
    public interface ILanguageBackend {
        void InitContext(string setName);
        void AddStructure(string setName, StructureDefinition sd);
        ShaderModel GetShaderModel(string setName);
        void AddFunction(string setName, ShaderFunctionAndBlockSyntax sf);
        void AddResource(string setName, ResourceDefinition rd);
        MethodProcessResult ProcessEntryFunction(string setName, ShaderFunction function);
    }
}