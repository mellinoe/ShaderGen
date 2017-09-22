using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ShaderGen.Tests
{
    public static class ShaderSetDiscovererTests
    {
        [Fact]
        public static void ShaderSetAutoDiscovery()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(compilation, new[] { backend });
            ShaderGenerationResult result = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> hlslSets = result.GetOutput(backend);
            Assert.Equal(3, hlslSets.Count);
            GeneratedShaderSet set = hlslSets[0];
            Assert.Equal("VertexAndFragment", set.Name);
            ShaderModel shaderModel = set.Model;
            ShaderFunction func = shaderModel.Functions[0];
            FxcTool.AssertCompilesCode(set.VertexShaderCode, "vs_5_0", "VS");
            FxcTool.AssertCompilesCode(set.FragmentShaderCode, "ps_5_0", "FS");
        }
    }
}
