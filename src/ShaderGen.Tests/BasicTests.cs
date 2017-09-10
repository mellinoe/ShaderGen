using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.IO;
using Xunit;

namespace ShaderGen.Tests
{
    public class BasicTests
    {
        [Fact]
        public void TestVertexToHlsl()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            SyntaxTree tree = TestUtil.GetSyntaxTree(compilation, "TestVertexShader.cs");
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);
            HlslBackend backend = new HlslBackend(semanticModel);
            ShaderModel shaderModel = ShaderGeneration.GetShaderModel(semanticModel, tree, backend);
            string code = backend.GetCode(shaderModel.GetFunction("VS"));
            File.WriteAllText("testoutput_vertex.hlsl", code);
        }

        [Fact]
        public void TestFragmentToHlsl()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            SyntaxTree tree = TestUtil.GetSyntaxTree(compilation, "TestFragmentShader.cs");
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);
            HlslBackend backend = new HlslBackend(semanticModel);
            ShaderModel shaderModel = ShaderGeneration.GetShaderModel(semanticModel, tree, backend);
            string code = backend.GetCode(shaderModel.GetFunction("FS"));
            File.WriteAllText("testoutput_fragment.hlsl", code);
        }

        [Fact]
        public void FxcCompilesTestVertex()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            SyntaxTree tree = TestUtil.GetSyntaxTree(compilation, "TestVertexShader.cs");
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);
            EmitResult result = compilation.Emit("testfile.il");
            Assert.True(result.Success);

            using (TempFile tempFile = new TempFile())
            {
                HlslBackend backend = new HlslBackend(semanticModel);
                ShaderModel shaderModel = ShaderGeneration.GetShaderModel(semanticModel, tree, backend);
                string code = backend.GetCode(shaderModel.GetFunction("VS"));
                File.WriteAllText(tempFile.FilePath, code);
                AssertHlslCompiler(tempFile.FilePath, "vs_5_0", "VS", tempFile.FilePath + ".bytes");
            }
        }

        private void AssertHlslCompiler(string file, string profile, string entryPoint, string output)
        {
            FxcToolResult result = FxcTool.Compile(file, profile, entryPoint, output);
            if (result.ExitCode != 0)
            {
                string message = result.StdOut + Environment.NewLine + result.StdError;
                throw new InvalidOperationException("HLSL compilation failed: " + message);
            }
        }
    }
}
