using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using TestShaders;
using Xunit;

namespace ShaderGen.Tests
{
    public class ShaderModelTests
    {
        [Fact]
        public void TestVertexShader_ShaderModel()
        {
            string functionName = "TestShaders.TestVertexShader.VS";
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                functionName,
                null,
                backend);
            ShaderGenerationResult genResult = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> sets = genResult.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            ShaderModel shaderModel = sets[0].Model;

            Assert.Equal(2, shaderModel.Structures.Length);
            Assert.Equal(3, shaderModel.Resources.Length);
            ShaderFunction vsEntry = shaderModel.GetFunction(functionName);
            Assert.Equal("VS", vsEntry.Name);
            Assert.Equal(1, vsEntry.Parameters.Length);
            Assert.True(vsEntry.IsEntryPoint);
            Assert.Equal(ShaderFunctionType.VertexEntryPoint, vsEntry.Type);
        }

        [Fact]
        public void TestVertexShader_VertexSemantics()
        {
            string functionName = "TestShaders.TestVertexShader.VS";
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                functionName,
                null,
                backend);
            ShaderGenerationResult genResult = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> sets = genResult.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            ShaderModel shaderModel = sets[0].Model;

            StructureDefinition vsInput = shaderModel.GetStructureDefinition(nameof(TestShaders) + "." + nameof(PositionTexture));
            Assert.Equal(SemanticType.Position, vsInput.Fields[0].SemanticType);
            Assert.Equal(SemanticType.TextureCoordinate, vsInput.Fields[1].SemanticType);

            StructureDefinition fsInput = shaderModel.GetStructureDefinition(
                nameof(TestShaders) + "." + nameof(TestVertexShader) + "." + nameof(TestVertexShader.VertexOutput));
            Assert.Equal(SemanticType.Position, fsInput.Fields[0].SemanticType);
            Assert.Equal(SemanticType.TextureCoordinate, fsInput.Fields[1].SemanticType);
        }

        [Fact]
        public void PartialFiles()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                "TestShaders.PartialVertex.VertexShaderFunc",
                null,
                backend);

            ShaderGenerationResult genResult = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> sets = genResult.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            GeneratedShaderSet set = sets[0];
            ShaderModel shaderModel = set.Model;
            string vsCode = set.VertexShaderCode;
            FxcTool.AssertCompilesCode(vsCode, "vs_5_0", "VertexShaderFunc");
        }
    }
}
