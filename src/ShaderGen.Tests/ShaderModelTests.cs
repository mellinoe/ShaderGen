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
                nameof(TestShaders) + "." + nameof(TestVertexShader) + "+" + nameof(TestVertexShader.VertexOutput));
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

        [Fact]
        public void PointLightsInfo_CorrectSize()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                "TestShaders.PointLightTestShaders.VS",
                null,
                backend);

            ShaderGenerationResult genResult = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> sets = genResult.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            GeneratedShaderSet set = sets[0];
            ShaderModel shaderModel = set.Model;

            Assert.Equal(1, shaderModel.Resources.Length);
            Assert.Equal(144, shaderModel.GetTypeSize(shaderModel.Resources[0].ValueType));
        }

        [Fact]
        public void MultipleResourceSets_CorrectlyParsed()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                "TestShaders.MultipleResourceSets.VS",
                null,
                backend);

            ShaderGenerationResult genResult = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> sets = genResult.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            GeneratedShaderSet set = sets[0];
            ShaderModel shaderModel = set.Model;

            Assert.Equal(13, shaderModel.Resources.Length);

            Assert.Equal(0, shaderModel.Resources[0].Set);
            Assert.Equal(0, shaderModel.Resources[0].Binding);
            Assert.Equal(0, shaderModel.Resources[1].Set);
            Assert.Equal(1, shaderModel.Resources[1].Binding);
            Assert.Equal(1, shaderModel.Resources[2].Set);
            Assert.Equal(0, shaderModel.Resources[2].Binding);
            Assert.Equal(2, shaderModel.Resources[3].Set);
            Assert.Equal(0, shaderModel.Resources[3].Binding);
            Assert.Equal(3, shaderModel.Resources[4].Set);
            Assert.Equal(0, shaderModel.Resources[4].Binding);
            Assert.Equal(4, shaderModel.Resources[5].Set);
            Assert.Equal(0, shaderModel.Resources[5].Binding);
            Assert.Equal(0, shaderModel.Resources[6].Set);
            Assert.Equal(2, shaderModel.Resources[6].Binding);

            Assert.Equal(0, shaderModel.Resources[7].Set);
            Assert.Equal(3, shaderModel.Resources[7].Binding);
            Assert.Equal(4, shaderModel.Resources[8].Set);
            Assert.Equal(1, shaderModel.Resources[8].Binding);
            Assert.Equal(0, shaderModel.Resources[9].Set);
            Assert.Equal(4, shaderModel.Resources[9].Binding);

            Assert.Equal(2, shaderModel.Resources[10].Set);
            Assert.Equal(1, shaderModel.Resources[10].Binding);
            Assert.Equal(0, shaderModel.Resources[11].Set);
            Assert.Equal(5, shaderModel.Resources[11].Binding);
            Assert.Equal(1, shaderModel.Resources[12].Set);
            Assert.Equal(1, shaderModel.Resources[12].Binding);
        }
    }
}
