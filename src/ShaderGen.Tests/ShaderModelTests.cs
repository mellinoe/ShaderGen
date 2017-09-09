using Microsoft.CodeAnalysis;
using TestShaders;
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
                Assert.True(shaderModel.EntryFunction.IsEntryPoint);
                Assert.Equal(ShaderFunctionType.VertexEntryPoint, shaderModel.EntryFunction.Type);
            }
        }

        [Fact]
        public void TestVertexShader_VertexSemantics()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            SyntaxTree tree = TestUtil.GetSyntaxTree(compilation, "TestVertexShader.cs");
            SemanticModel model = compilation.GetSemanticModel(tree);
            using (TempFile tmp = new TempFile())
            {
                ShaderModel shaderModel = ShaderGeneration.GenerateHlsl(model, tree, tmp.FilePath);

                StructureDefinition vsInput = shaderModel.GetStructureDefinition(nameof(TestShaders) + "." + nameof(PositionTexture));
                Assert.Equal(SemanticType.Position, vsInput.Fields[0].SemanticType);
                Assert.Equal(SemanticType.TextureCoordinate, vsInput.Fields[1].SemanticType);

                StructureDefinition fsInput = shaderModel.GetStructureDefinition(
                    nameof(TestShaders) + "." + nameof(TestVertexShader) + "." + nameof(TestVertexShader.VertexOutput));
                Assert.Equal(SemanticType.Position, fsInput.Fields[0].SemanticType);
                Assert.Equal(SemanticType.TextureCoordinate, fsInput.Fields[1].SemanticType);
            }
        }
    }
}
