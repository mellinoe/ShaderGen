using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    /// <summary>
    /// Entry-point for generating GPU shader code from C# semantic models.
    /// </summary>
    public class ShaderGeneration
    {
        public static ShaderModel GetShaderModel(SemanticModel model, SyntaxTree tree, LanguageBackend backend)
        {
            ShaderSyntaxWalker walker = new ShaderSyntaxWalker(model, backend);
            ShaderModel ret = walker.GetShaderModel(tree);
            return ret;
        }
    }
}
