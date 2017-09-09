using Microsoft.CodeAnalysis;
using Xunit;

namespace ShaderGen.Tests
{
    public class ShaderModelTests
    {
        [Fact]
        public void TestVertexShader_ShaderModel()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            SyntaxTree tree = TestUtil.GetSyntaxTree(compilation, "TestVertexShader.cs");
            SemanticModel model = compilation.GetSemanticModel(tree);
            using (TempFile tmp = new TempFile())
            {
                ShaderModel shaderModel = ShaderGeneration.GenerateHlsl(model, tree, tmp.FilePath);
                Assert.Equal(2, shaderModel.Structures.Length);
                Assert.Equal(3, shaderModel.Uniforms.Length);
                Assert.Equal("VS", shaderModel.EntryFunction.Name);
                Assert.Equal(1, shaderModel.EntryFunction.Parameters.Length);
            }
        }
    }
}
