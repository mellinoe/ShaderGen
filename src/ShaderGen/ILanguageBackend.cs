namespace ShaderGen {
    public interface ILanguageBackend {
        string GeneratedFileExtension { get; }

        bool CompilationToolsAreAvailable();
        bool CompileCode(string shaderPath, string entryPoint, ShaderFunctionType type, out string path);
        
        void InitContext(string setName);
        void AddStructure(string setName, StructureDefinition sd);
        ShaderModel GetShaderModel(string setName);
        void AddFunction(string setName, ShaderFunctionAndBlockSyntax sf);
        void AddResource(string setName, ResourceDefinition rd);
        MethodProcessResult ProcessEntryFunction(string setName, ShaderFunction function);
    }
}