using Microsoft.CodeAnalysis;
using System;
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
            SemanticModel model = compilation.GetSemanticModel(tree);
            ShaderGeneration.GenerateHlsl(model, tree, "testoutput.hlsl");
        }

        [Fact]
        public void FxcCompilesTestVertex()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            SyntaxTree tree = TestUtil.GetSyntaxTree(compilation, "TestVertexShader.cs");
            SemanticModel model = compilation.GetSemanticModel(tree);
            compilation.Emit("testfile.il");

            using (TempFile tempFile = new TempFile())
            {
                ShaderGeneration.GenerateHlsl(model, tree, tempFile.FilePath);
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
