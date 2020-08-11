using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using ShaderGen.Hlsl;
using ShaderGen.Tests.Tools;
using Xunit;

namespace ShaderGen.Tests
{
    public static class ShaderSetDiscovererTests
    {
        [SkippableFact(typeof(RequiredToolFeatureMissingException))]
        public static void ShaderSetAutoDiscovery()
        {
            ToolChain toolChain = ToolChain.Get(ToolFeatures.ToCompiled);
            if (toolChain == null)
            {
                throw new RequiredToolFeatureMissingException("No tool chain supporting compilation was found!");
            }

            Compilation compilation = TestUtil.GetCompilation();
            LanguageBackend backend = toolChain.CreateBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(compilation, backend);
            ShaderGenerationResult generationResult = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> hlslSets = generationResult.GetOutput(backend);
            Assert.Equal(4, hlslSets.Count);
            GeneratedShaderSet set = hlslSets[0];
            
            //Updated to new naming convention
            Assert.Equal(set.VertexFunction.Name + "+" + set.FragmentFunction.Name, set.Name);

            CompileResult result = toolChain.Compile(set.VertexShaderCode, Stage.Vertex, "VS");
            Assert.False(result.HasError, result.ToString());

            result = toolChain.Compile(set.FragmentShaderCode, Stage.Fragment, "FS");
            Assert.False(result.HasError, result.ToString());
        }
    }
}
