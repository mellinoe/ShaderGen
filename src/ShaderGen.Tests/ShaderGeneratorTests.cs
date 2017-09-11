using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ShaderGen.Tests
{
    public class ShaderGeneratorTests
    {
        [Fact]
        public void Basic()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                "TestShaders.VertexAndFragment.VS",
                "TestShaders.VertexAndFragment.FS",
                backend);

            ShaderModel shaderModel = sg.GenerateShaders();
            string vsCode = backend.GetCode(shaderModel.GetFunction("VS"));
            FxcTool.AssertCompilesCode(vsCode, "vs_5_0", "VS");
            string fsCode = backend.GetCode(shaderModel.GetFunction("FS"));
            FxcTool.AssertCompilesCode(fsCode, "ps_5_0", "FS");
        }

        [Fact]
        public void PartialFiles()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                "TestShaders.PartialVertex.VertexShader",
                null,
                backend);

            ShaderModel shaderModel = sg.GenerateShaders();
            ShaderFunction entryFunction = shaderModel.GetFunction("VertexShaderFunc");
            string vsCode = backend.GetCode(entryFunction);
            FxcTool.AssertCompilesCode(vsCode, "vs_5_0", entryFunction.Name);
        }
    }
}
