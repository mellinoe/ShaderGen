using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ShaderGen.Tests
{
    public class ShaderGeneratorTests
    {
        [Theory]
        [InlineData("TestShaders.TestVertexShader.VS", null)]
        [InlineData(null, "TestShaders.TestFragmentShader.FS")]
        [InlineData("TestShaders.TestVertexShader.VS", "TestShaders.TestFragmentShader.FS")]
        [InlineData(null, "TestShaders.TextureSamplerFragment.FS")]
        [InlineData("TestShaders.VertexAndFragment.VS", "TestShaders.VertexAndFragment.FS")]
        [InlineData(null, "TestShaders.ComplexExpression.FS")]
        [InlineData("TestShaders.PartialVertex.VertexShaderFunc", null)]
        [InlineData("TestShaders.VeldridShaders.ForwardMtlCombined.VS", "TestShaders.VeldridShaders.ForwardMtlCombined.FS")]
        [InlineData("TestShaders.VeldridShaders.ForwardMtlCombined.VS", null)]
        [InlineData(null, "TestShaders.VeldridShaders.ForwardMtlCombined.FS")]
        public void HlslEndToEnd(string vsName, string fsName)
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                vsName,
                fsName,
                backend);

            ShaderGenerationResult result = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> sets = result.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            GeneratedShaderSet set = sets[0];
            ShaderModel shaderModel = set.Model;

            if (vsName != null)
            {
                ShaderFunction vsFunction = shaderModel.GetFunction(vsName);
                string vsCode = set.VertexShaderCode;
                FxcTool.AssertCompilesCode(vsCode, "vs_5_0", vsFunction.Name);
            }
            if (fsName != null)
            {
                ShaderFunction fsFunction = shaderModel.GetFunction(fsName);
                string fsCode = set.FragmentShaderCode;
                FxcTool.AssertCompilesCode(fsCode, "ps_5_0", fsFunction.Name);
            }
        }

        [Theory]
        [InlineData("TestShaders.TestVertexShader.VS", null)]
        [InlineData(null, "TestShaders.TestFragmentShader.FS")]
        [InlineData("TestShaders.TestVertexShader.VS", "TestShaders.TestFragmentShader.FS")]
        [InlineData(null, "TestShaders.TextureSamplerFragment.FS")]
        [InlineData("TestShaders.VertexAndFragment.VS", "TestShaders.VertexAndFragment.FS")]
        [InlineData(null, "TestShaders.ComplexExpression.FS")]
        [InlineData("TestShaders.PartialVertex.VertexShaderFunc", null)]
        [InlineData("TestShaders.VeldridShaders.ForwardMtlCombined.VS", "TestShaders.VeldridShaders.ForwardMtlCombined.FS")]
        [InlineData("TestShaders.VeldridShaders.ForwardMtlCombined.VS", null)]
        [InlineData(null, "TestShaders.VeldridShaders.ForwardMtlCombined.FS")]
        public void Glsl330EndToEnd(string vsName, string fsName)
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            Glsl330Backend backend = new Glsl330Backend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                vsName,
                fsName,
                backend);

            ShaderGenerationResult result = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> sets = result.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            GeneratedShaderSet set = sets[0];
            ShaderModel shaderModel = set.Model;

            if (vsName != null)
            {
                ShaderFunction vsFunction = shaderModel.GetFunction(vsName);
                string vsCode = set.VertexShaderCode;
                GlsLangValidatorTool.AssertCompilesCode(vsCode, "vert", false);
            }
            if (fsName != null)
            {
                ShaderFunction fsFunction = shaderModel.GetFunction(fsName);
                string fsCode = set.FragmentShaderCode;
                GlsLangValidatorTool.AssertCompilesCode(fsCode, "frag", false);
            }
        }

        [Theory]
        [InlineData("TestShaders.TestVertexShader.VS", null)]
        [InlineData(null, "TestShaders.TestFragmentShader.FS")]
        [InlineData("TestShaders.TestVertexShader.VS", "TestShaders.TestFragmentShader.FS")]
        [InlineData(null, "TestShaders.TextureSamplerFragment.FS")]
        [InlineData("TestShaders.VertexAndFragment.VS", "TestShaders.VertexAndFragment.FS")]
        [InlineData(null, "TestShaders.ComplexExpression.FS")]
        [InlineData("TestShaders.PartialVertex.VertexShaderFunc", null)]
        [InlineData("TestShaders.VeldridShaders.ForwardMtlCombined.VS", "TestShaders.VeldridShaders.ForwardMtlCombined.FS")]
        [InlineData("TestShaders.VeldridShaders.ForwardMtlCombined.VS", null)]
        [InlineData(null, "TestShaders.VeldridShaders.ForwardMtlCombined.FS")]
        public void Glsl450EndToEnd(string vsName, string fsName)
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            LanguageBackend backend = new Glsl450Backend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                vsName,
                fsName,
                backend);

            ShaderGenerationResult result = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> sets = result.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            GeneratedShaderSet set = sets[0];
            ShaderModel shaderModel = set.Model;

            if (vsName != null)
            {
                ShaderFunction vsFunction = shaderModel.GetFunction(vsName);
                string vsCode = set.VertexShaderCode;
                GlsLangValidatorTool.AssertCompilesCode(vsCode, "vert", true);
            }
            if (fsName != null)
            {
                ShaderFunction fsFunction = shaderModel.GetFunction(fsName);
                string fsCode = set.FragmentShaderCode;
                GlsLangValidatorTool.AssertCompilesCode(fsCode, "frag", true);
            }
        }

        public void DummyTest()
        {
            string vsName = "TestShaders.VeldridShaders.VertexAndFragment.VS";
            string fsName = "TestShaders.VeldridShaders.VertexAndFragment.FS";
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            using (TempFile fp = new TempFile())
            {
                Microsoft.CodeAnalysis.Emit.EmitResult emitResult = compilation.Emit(fp);
                Assert.True(emitResult.Success);
            }

            LanguageBackend backend = new Glsl450Backend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                vsName,
                fsName,
                backend);

            ShaderGenerationResult result = sg.GenerateShaders();
            IReadOnlyList<GeneratedShaderSet> sets = result.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            GeneratedShaderSet set = sets[0];
            ShaderModel shaderModel = set.Model;

            if (vsName != null)
            {
                ShaderFunction vsFunction = shaderModel.GetFunction(vsName);
                string vsCode = set.VertexShaderCode;
                File.WriteAllText(@"C:\Users\raver\Documents\forward-vertex.glsl", vsCode);
                GlsLangValidatorTool.AssertCompilesCode(vsCode, "vert", true);
            }
            if (fsName != null)
            {
                ShaderFunction fsFunction = shaderModel.GetFunction(fsName);
                string fsCode = set.FragmentShaderCode;
                File.WriteAllText(@"C:\Users\raver\Documents\forward-frag.glsl", fsCode);
                GlsLangValidatorTool.AssertCompilesCode(fsCode, "frag", true);
            }
        }
    }
}
