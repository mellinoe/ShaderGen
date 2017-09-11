using Microsoft.CodeAnalysis;
using System.IO;
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
            HlslBackend backend = new HlslBackend(model);
            ShaderModel shaderModel = ShaderGeneration.GetShaderModel(model, tree, backend);
            Assert.Equal(2, shaderModel.Structures.Length);
            Assert.Equal(3, shaderModel.Resources.Length);
            ShaderFunction vsEntry = shaderModel.GetFunction("VS");
            Assert.Equal("VS", vsEntry.Name);
            Assert.Equal(1, vsEntry.Parameters.Length);
            Assert.True(vsEntry.IsEntryPoint);
            Assert.Equal(ShaderFunctionType.VertexEntryPoint, vsEntry.Type);
        }

        [Fact]
        public void TestVertexShader_VertexSemantics()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            SyntaxTree tree = TestUtil.GetSyntaxTree(compilation, "TestVertexShader.cs");
            SemanticModel model = compilation.GetSemanticModel(tree);
            ShaderModel shaderModel = ShaderGeneration.GetShaderModel(model, tree, new HlslBackend(model));

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
