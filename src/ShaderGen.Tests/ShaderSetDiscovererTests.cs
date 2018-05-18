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
            ShaderGenerator sg = new ShaderGenerator(compilation, backend);
            ShaderGenerationResult generationResult = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> hlslSets = generationResult.GetOutput(backend);
            Assert.Equal(4, hlslSets.Count);
            GeneratedShaderSet set = hlslSets[0];
            Assert.Equal("VertexAndFragment", set.Name);

            ToolResult result = ToolChain.Hlsl.Compile(set.VertexShaderCode, Stage.Vertex, "VS");
            Assert.False(result.HasError, result.ToString());

            result = ToolChain.Hlsl.Compile(set.FragmentShaderCode, Stage.Fragment, "FS");
            Assert.False(result.HasError, result.ToString());
        }
    }
}
