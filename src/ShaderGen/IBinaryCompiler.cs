namespace ShaderGen {
    public interface IBinaryCompiler {
        string GeneratedFileExtension { get; }

        bool CompilationToolsAreAvailable();
        bool CompileCode(string shaderPath, string entryPoint, ShaderFunctionType type, out string path);
        
        LanguageBackend Language { get; }
    }
}