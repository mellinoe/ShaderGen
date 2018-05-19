using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
using ShaderGen.Tests.Tools;
using TestShaders;
using Xunit;

namespace ShaderGen.Tests
{
    public class ShaderBuiltinsTests
    {
        [Fact]
        public void TestShaderBuiltins_Compute()
        {
            string csFunctionName = $"TestShaders.ShaderBuiltinsComputeTest.CS";
            Compilation compilation = TestUtil.GetTestProjectCompilation();

            // Get backends for every toolchain that is available
            LanguageBackend[] backends = new[] { ToolChain.Glsl330.CreateBackend(compilation) };
            /* This is how yet get all, for now we're just using Glsl330
             ToolChain.All
            .Where(t => t.IsAvailable)
            .Select(t => t.CreateBackend(compilation))
            .ToArray();
            */
            ShaderSetProcessor processor = new ShaderSetProcessor();

            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                backends,
                null, null, csFunctionName, processor);
            ShaderGenerationResult genResult = sg.GenerateShaders();
            /*
            IReadOnlyList<GeneratedShaderSet> sets = genResult.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            ShaderModel shaderModel = sets[0].Model;

            Assert.Equal(2, shaderModel.Structures.Length);
            Assert.Equal(3, shaderModel.AllResources.Length);
            ShaderFunction vsEntry = shaderModel.GetFunction(csFunctionName);
            Assert.Equal("VS", vsEntry.Name);
            Assert.Single(vsEntry.Parameters);
            Assert.True(vsEntry.IsEntryPoint);
            Assert.Equal(ShaderFunctionType.VertexEntryPoint, vsEntry.Type);*/
        }
        private class ShaderSetProcessor : IShaderSetProcessor
        {
            public string Result { get; private set; }

            public string UserArgs { get; set; }

            public void ProcessShaderSet(ShaderSetProcessorInput input)
            {
                Result = string.Join(" ", input.Model.AllResources.Select(rd => rd.Name));
            }
        }
    }
}