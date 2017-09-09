using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    /// <summary>
    /// Entry-point for generating GPU shader code from C# semantic models.
    /// </summary>
    public class ShaderGeneration
    {
        public static void GenerateHlsl(SemanticModel model, SyntaxTree tree, string outputPath)
        {
            ShaderSyntaxWalker walker = new ShaderSyntaxWalker(model, new HlslBackend(model));
            walker.Visit(tree.GetRoot());
            walker.WriteToFile(outputPath);
        }
    }
}
