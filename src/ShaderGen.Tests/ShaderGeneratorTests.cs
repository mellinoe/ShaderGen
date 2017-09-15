using Microsoft.CodeAnalysis;
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

            ShaderModel shaderModel = sg.GenerateShaders();
            if (vsName != null)
            {
                ShaderFunction vsFunction = shaderModel.GetFunction(vsName);
                string vsCode = backend.GetCode(vsFunction);
                FxcTool.AssertCompilesCode(vsCode, "vs_5_0", vsFunction.Name);
            }
            if (fsName != null)
            {
                ShaderFunction fsFunction = shaderModel.GetFunction(fsName);
                string fsCode = backend.GetCode(fsFunction);
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

            ShaderModel shaderModel = sg.GenerateShaders();
            if (vsName != null)
            {
                ShaderFunction vsFunction = shaderModel.GetFunction(vsName);
                string vsCode = backend.GetCode(vsFunction);
                GlsLangValidatorTool.AssertCompilesCode(vsCode, "vert", false);
            }
            if (fsName != null)
            {
                ShaderFunction fsFunction = shaderModel.GetFunction(fsName);
                string fsCode = backend.GetCode(fsFunction);
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

            ShaderModel shaderModel = sg.GenerateShaders();
            if (vsName != null)
            {
                ShaderFunction vsFunction = shaderModel.GetFunction(vsName);
                string vsCode = backend.GetCode(vsFunction);
                GlsLangValidatorTool.AssertCompilesCode(vsCode, "vert", true);
            }
            if (fsName != null)
            {
                ShaderFunction fsFunction = shaderModel.GetFunction(fsName);
                string fsCode = backend.GetCode(fsFunction);
                GlsLangValidatorTool.AssertCompilesCode(fsCode, "frag", true);
            }
        }

        public void DummyTest()
        {
            string vsName = "TestShaders.VeldridShaders.ForwardMtlCombined.VS";
            string fsName = "TestShaders.VeldridShaders.ForwardMtlCombined.FS";
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            using (TempFile fp = new TempFile())
            {
                Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(fp);
                Assert.True(result.Success);
            }

            LanguageBackend backend = new Glsl450Backend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                vsName,
                fsName,
                backend);

            ShaderModel shaderModel = sg.GenerateShaders();
            if (vsName != null)
            {
                ShaderFunction vsFunction = shaderModel.GetFunction(vsName);
                string vsCode = backend.GetCode(vsFunction);
                File.WriteAllText(@"C:\Users\raver\Documents\forward-vertex.glsl", vsCode);
                GlsLangValidatorTool.AssertCompilesCode(vsCode, "vert", true);
            }
            if (fsName != null)
            {
                ShaderFunction fsFunction = shaderModel.GetFunction(fsName);
                string fsCode = backend.GetCode(fsFunction);
                File.WriteAllText(@"C:\Users\raver\Documents\forward-frag.glsl", fsCode);
                GlsLangValidatorTool.AssertCompilesCode(fsCode, "frag", true);
            }
        }
    }
}
