using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using ShaderGen.Hlsl;
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
            Assert.Equal(3, shaderModel.AllResources.Length);
            ShaderFunction vsEntry = shaderModel.GetFunction(functionName);
            Assert.Equal("VS", vsEntry.Name);
            Assert.Single(vsEntry.Parameters);
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
            Assert.Equal(SemanticType.SystemPosition, fsInput.Fields[0].SemanticType);
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

            Assert.Single(shaderModel.AllResources);
            Assert.Equal(144, shaderModel.GetTypeSize(shaderModel.AllResources[0].ValueType));
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

            Assert.Equal(13, shaderModel.AllResources.Length);

            Assert.Equal(0, shaderModel.AllResources[0].Set);
            Assert.Equal(0, shaderModel.AllResources[0].Binding);
            Assert.Equal(0, shaderModel.AllResources[1].Set);
            Assert.Equal(1, shaderModel.AllResources[1].Binding);
            Assert.Equal(1, shaderModel.AllResources[2].Set);
            Assert.Equal(0, shaderModel.AllResources[2].Binding);
            Assert.Equal(2, shaderModel.AllResources[3].Set);
            Assert.Equal(0, shaderModel.AllResources[3].Binding);
            Assert.Equal(3, shaderModel.AllResources[4].Set);
            Assert.Equal(0, shaderModel.AllResources[4].Binding);
            Assert.Equal(4, shaderModel.AllResources[5].Set);
            Assert.Equal(0, shaderModel.AllResources[5].Binding);
            Assert.Equal(0, shaderModel.AllResources[6].Set);
            Assert.Equal(2, shaderModel.AllResources[6].Binding);

            Assert.Equal(0, shaderModel.AllResources[7].Set);
            Assert.Equal(3, shaderModel.AllResources[7].Binding);
            Assert.Equal(4, shaderModel.AllResources[8].Set);
            Assert.Equal(1, shaderModel.AllResources[8].Binding);
            Assert.Equal(0, shaderModel.AllResources[9].Set);
            Assert.Equal(4, shaderModel.AllResources[9].Binding);

            Assert.Equal(2, shaderModel.AllResources[10].Set);
            Assert.Equal(1, shaderModel.AllResources[10].Binding);
            Assert.Equal(0, shaderModel.AllResources[11].Set);
            Assert.Equal(5, shaderModel.AllResources[11].Binding);
            Assert.Equal(1, shaderModel.AllResources[12].Set);
            Assert.Equal(1, shaderModel.AllResources[12].Binding);
        }

        [Fact]
        public void ResourcesUsedInStages()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                "TestShaders.UsedResourcesShaders.VS",
                "TestShaders.UsedResourcesShaders.FS",
                backend);

            ShaderGenerationResult genResult = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> sets = genResult.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            GeneratedShaderSet set = sets[0];
            ShaderModel shaderModel = set.Model;

            Assert.Equal(4, shaderModel.VertexResources.Length);
            Assert.Contains(shaderModel.VertexResources, rd => rd.Name == "VS_M0");
            Assert.Contains(shaderModel.VertexResources, rd => rd.Name == "VS_M1");
            Assert.Contains(shaderModel.VertexResources, rd => rd.Name == "VS_T0");
            Assert.Contains(shaderModel.VertexResources, rd => rd.Name == "VS_S0");

            Assert.Equal(5, shaderModel.FragmentResources.Length);
            Assert.Contains(shaderModel.FragmentResources, rd => rd.Name == "FS_M0");
            Assert.Contains(shaderModel.FragmentResources, rd => rd.Name == "FS_M1");
            Assert.Contains(shaderModel.FragmentResources, rd => rd.Name == "FS_T0");
            Assert.Contains(shaderModel.FragmentResources, rd => rd.Name == "FS_S0");
            Assert.Contains(shaderModel.FragmentResources, rd => rd.Name == "FS_M2_Indirect");
        }
    }
}
