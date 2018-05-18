using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using ShaderGen.Hlsl;
using ShaderGen.Tests.Attributes;
using ShaderGen.Tests.Tools;
using Xunit;

namespace ShaderGen.Tests
{
    public static class ShaderSetDiscovererTests
    {
        [HlslFact]
        public static void ShaderSetAutoDiscovery()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(compilation, new[] { backend });
            ShaderGenerationResult result = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> hlslSets = result.GetOutput(backend);
            Assert.Equal(4, hlslSets.Count);
            GeneratedShaderSet set = hlslSets[0];
            Assert.Equal("VertexAndFragment", set.Name);
            ShaderModel shaderModel = set.Model;
            ShaderFunction func = shaderModel.Functions[0];
            FxcTool.AssertCompilesCode(set.VertexShaderCode, "vs_5_0", "VS");
            FxcTool.AssertCompilesCode(set.FragmentShaderCode, "ps_5_0", "FS");
        }
    }
}
