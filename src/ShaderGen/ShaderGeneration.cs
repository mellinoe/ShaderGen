using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    // TODO: REPLACE THIS WITH BUILDER-BASED ShaderGenerator.
    /// <summary>
    /// Entry-point for generating GPU shader code from C# semantic models.
    /// </summary>
    public class ShaderGeneration
    {
        public static ShaderModel GetShaderModel(SemanticModel model, SyntaxTree tree, LanguageBackend backend)
        {
            ShaderSyntaxWalker walker = new ShaderSyntaxWalker(model.Compilation, backend);
            ShaderModel ret = walker.GetShaderModel(tree);
            return ret;
        }
    }
}
