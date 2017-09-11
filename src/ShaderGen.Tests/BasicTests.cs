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
            HlslBackend backend = new HlslBackend(compilation);
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
            HlslBackend backend = new HlslBackend(compilation);
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
                HlslBackend backend = new HlslBackend(compilation);
                ShaderModel shaderModel = ShaderGeneration.GetShaderModel(semanticModel, tree, backend);
                string code = backend.GetCode(shaderModel.GetFunction("VS"));
                File.WriteAllText(tempFile.FilePath, code);
                FxcTool.AssertCompilesFile(tempFile.FilePath, "vs_5_0", "VS");
            }
        }

        [Fact]
        public void VertexAndFragment_SameFile()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            SyntaxTree tree = TestUtil.GetSyntaxTree(compilation, "VertexAndFragment.cs");
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            using (TempFile vsOut = new TempFile())
            using (TempFile fsOut = new TempFile())
            {
                HlslBackend backend = new HlslBackend(compilation);
                ShaderModel shaderModel = ShaderGeneration.GetShaderModel(semanticModel, tree, backend);

                ShaderFunction vsFunc = shaderModel.GetFunction("VS");
                Assert.Equal(ShaderFunctionType.VertexEntryPoint, vsFunc.Type);
                string vs = backend.GetCode(vsFunc);
                File.WriteAllText(vsOut.FilePath, vs);
                FxcTool.AssertCompilesFile(vsOut.FilePath, "vs_5_0", "VS");

                ShaderFunction fsFunc = shaderModel.GetFunction("FS");
                Assert.Equal(ShaderFunctionType.FragmentEntryPoint, fsFunc.Type);
                string fs = backend.GetCode(fsFunc);
                File.WriteAllText(fsOut.FilePath, fs);
                FxcTool.AssertCompilesFile(fsOut.FilePath, "ps_5_0", "FS");
            }
        }

        [Fact]
        public void FragmentWithTextureAndSampler()
        {
            GetModels("TextureSamplerFragment.cs", "FS", out LanguageBackend backend, out ShaderModel shaderModel, out ShaderFunction function);
            string code = backend.GetCode(function);
            using (TempFile tf = new TempFile())
            {
                File.WriteAllText(tf, code);
                FxcTool.AssertCompilesFile(tf, "ps_5_0", "FS");
            }
        }

        [Fact]
        public void ComplexExpression()
        {
            GetModels("ComplexExpression.cs", "FS", out LanguageBackend backend, out ShaderModel shaderModel, out ShaderFunction function);
            string code = backend.GetCode(function);
            using (TempFile tf = new TempFile())
            {
                File.WriteAllText(tf, code);
                FxcTool.AssertCompilesFile(tf, "ps_5_0", "FS");
            }
        }

        private void GetModels(
            string sourceFile,
            string entryFunctionName,
            out LanguageBackend backend,
            out ShaderModel shaderModel,
            out ShaderFunction function)
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            SyntaxTree tree = TestUtil.GetSyntaxTree(compilation, sourceFile);
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);
            backend = new HlslBackend(compilation);
            shaderModel = ShaderGeneration.GetShaderModel(semanticModel, tree, backend);
            function = shaderModel.GetFunction(entryFunctionName);
        }
    }
}
