using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
using ShaderGen.Tests.Tools;
using TestShaders;
using Xunit;
using Xunit.Abstractions;

namespace ShaderGen.Tests
{
    public class ShaderBuiltinsTests
    {
        private readonly ITestOutputHelper _output;

        public ShaderBuiltinsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestShaderBuiltins_Compute()
        {
            string csFunctionName = "TestShaders.ShaderBuiltinsComputeTest.CS";
            Compilation compilation = TestUtil.GetTestProjectCompilation();

            // Get backends for every toolchain that is available
            LanguageBackend[] backends = // new[] { ToolChain.Glsl330.CreateBackend(compilation) };
            // This is how yet get all, for now we're just using Glsl330
             ToolChain.All
            .Where(t => t.IsAvailable)
            .Select(t => t.CreateBackend(compilation))
            .ToArray();

            ShaderSetProcessor processor = new ShaderSetProcessor();

            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                backends,
                null,
                null,
                csFunctionName,
                processor);

            ShaderGenerationResult generationResult = sg.GenerateShaders();

            string spacer1 = new string('=', 80);
            string spacer2 = new string('-', 80);

            bool failed = false;
            foreach (LanguageBackend backend in backends)
            {
                ToolChain toolChain = ToolChain.Get(backend);
                GeneratedShaderSet set = generationResult.GetOutput(backend).Single();
                _output.WriteLine(spacer1);
                _output.WriteLine($"Generated shader set for {toolChain.Name} backend.");

                ToolResult result = toolChain.Compile(set.ComputeShaderCode, Stage.Compute, set.ComputeFunction.Name);
                if (result.HasError)
                {
                    _output.WriteLine($"Failed to compile Compute Shader from set \"{set.Name}\"!");
                    _output.WriteLine(result.ToString());
                    failed = true;
                }
                else
                    _output.WriteLine($"Compiled Compute Shader from set \"{set.Name}\"!");

                _output.WriteLine(string.Empty);
            }

            Assert.False(failed);
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