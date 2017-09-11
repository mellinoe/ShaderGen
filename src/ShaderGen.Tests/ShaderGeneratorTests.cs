using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
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
    }
}
