using System;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
using ShaderGen.Metal;
using ShaderGen.Tests.Tools;
using Veldrid;
using Xunit;
using Xunit.Abstractions;

namespace ShaderGen.Tests
{
    public class ShaderGeneratorTests
    {
        private readonly ITestOutputHelper _output;

        public ShaderGeneratorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> ShaderSets()
        {
            yield return new object[] { "TestShaders.TestVertexShader.VS", null };
            yield return new object[] { null, "TestShaders.TestFragmentShader.FS" };
            yield return new object[] { "TestShaders.TestVertexShader.VS", "TestShaders.TestFragmentShader.FS" };
            yield return new object[] { null, "TestShaders.TextureSamplerFragment.FS" };
            yield return new object[] { "TestShaders.VertexAndFragment.VS", "TestShaders.VertexAndFragment.FS" };
            yield return new object[] { null, "TestShaders.ComplexExpression.FS" };
            yield return new object[] { "TestShaders.PartialVertex.VertexShaderFunc", null };
            yield return new object[] { "TestShaders.VeldridShaders.ForwardMtlCombined.VS", "TestShaders.VeldridShaders.ForwardMtlCombined.FS" };
            yield return new object[] { "TestShaders.VeldridShaders.ForwardMtlCombined.VS", null };
            yield return new object[] { null, "TestShaders.VeldridShaders.ForwardMtlCombined.FS" };
            yield return new object[] { "TestShaders.CustomStructResource.VS", null };
            yield return new object[] { "TestShaders.Swizzles.VS", null };
            yield return new object[] { "TestShaders.CustomMethodCalls.VS", null };
            yield return new object[] { "TestShaders.VeldridShaders.ShadowDepth.VS", "TestShaders.VeldridShaders.ShadowDepth.FS" };
            yield return new object[] { "TestShaders.ShaderBuiltinsTestShader.VS", null };
            yield return new object[] { null, "TestShaders.ShaderBuiltinsTestShader.FS" };
            yield return new object[] { "TestShaders.VectorConstructors.VS", null };
            yield return new object[] { "TestShaders.VectorIndexers.VS", null };
            yield return new object[] { "TestShaders.VectorStaticProperties.VS", null };
            yield return new object[] { "TestShaders.VectorStaticFunctions.VS", null };
            yield return new object[] { "TestShaders.MultipleResourceSets.VS", null };
            yield return new object[] { "TestShaders.MultipleColorOutputs.VS", "TestShaders.MultipleColorOutputs.FS" };
            yield return new object[] { "TestShaders.MultisampleTexture.VS", null };
            yield return new object[] { "TestShaders.BuiltInVariables.VS", null };
            yield return new object[] { "TestShaders.MathFunctions.VS", null };
            yield return new object[] { "TestShaders.Matrix4x4Members.VS", null };
            yield return new object[] { "TestShaders.CustomMethodUsingUniform.VS", null };
            yield return new object[] { "TestShaders.PointLightTestShaders.VS", null };
            yield return new object[] { "TestShaders.UIntVectors.VS", null };
            yield return new object[] { "TestShaders.VeldridShaders.UIntVertexAttribs.VS", null };
            yield return new object[] { "TestShaders.SwitchStatements.VS", null };
            yield return new object[] { "TestShaders.VariableTypes.VS", null };
            yield return new object[] { "TestShaders.OutParameters.VS", null };
            yield return new object[] { null, "TestShaders.ExpressionBodiedMethods.ExpressionBodyWithReturn" };
            yield return new object[] { null, "TestShaders.ExpressionBodiedMethods.ExpressionBodyWithoutReturn" };
            yield return new object[] { "TestShaders.StructuredBufferTestShader.VS", null };
            yield return new object[] { null, "TestShaders.StructuredBufferTestShader.FS"};
            yield return new object[] { null, "TestShaders.DepthTextureSamplerFragment.FS" };
            yield return new object[] { null, "TestShaders.Enums.FS" };
            yield return new object[] { "TestShaders.VertexWithStructuredBuffer.VS", null };
            yield return new object[] { "TestShaders.WhileAndDoWhile.VS", null };
        }

        public static IEnumerable<object[]> ComputeShaders()
        {
            yield return new object[] { "TestShaders.SimpleCompute.CS" };
            yield return new object[] { "TestShaders.ComplexCompute.CS" };
        }

        private static readonly HashSet<string> s_glslesSkippedShaders = new HashSet<string>()
        {
            "TestShaders.ComplexCompute.CS"
        };

        private void TestCompile(GraphicsBackend graphicsBackend, string vsName, string fsName, string csName = null)
        { 
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            ToolChain toolChain = ToolChain.Require(ToolFeatures.ToCompiled, graphicsBackend).Single();

            LanguageBackend backend = toolChain.CreateBackend(compilation);
            ShaderGenerator sg = new ShaderGenerator(compilation, backend, vsName, fsName, csName);

            ShaderGenerationResult generationResult = sg.GenerateShaders();

            IReadOnlyList<GeneratedShaderSet> sets = generationResult.GetOutput(backend);
            Assert.Equal(1, sets.Count);
            GeneratedShaderSet set = sets[0];
            ShaderModel shaderModel = set.Model;

            List<CompileResult> results = new List<CompileResult>();
            if (!string.IsNullOrWhiteSpace(vsName))
            {
                ShaderFunction vsFunction = shaderModel.GetFunction(vsName);
                string vsCode = set.VertexShaderCode;

                results.Add(toolChain.Compile(vsCode, Stage.Vertex, vsFunction.Name));
            }
            if (!string.IsNullOrWhiteSpace(fsName))
            {
                ShaderFunction fsFunction = shaderModel.GetFunction(fsName);
                string fsCode = set.FragmentShaderCode;
                results.Add(toolChain.Compile(fsCode, Stage.Fragment, fsFunction.Name));
            }
            if (!string.IsNullOrWhiteSpace(csName))
            {
                ShaderFunction csFunction = shaderModel.GetFunction(csName);
                string csCode = set.ComputeShaderCode;
                results.Add(toolChain.Compile(csCode, Stage.Compute, csFunction.Name));
            }

            // Collate results
            StringBuilder builder = new StringBuilder();
            foreach (CompileResult result in results)
                if (result.HasError)
                    builder.AppendLine(result.ToString());

            Assert.True(builder.Length < 1, builder.ToString());
        }

        [SkippableTheory(typeof(RequiredToolFeatureMissingException))]
        [MemberData(nameof(ShaderSets))]
        public void HlslCompile(string vsName, string fsName) => TestCompile(GraphicsBackend.Direct3D11, vsName, fsName);

        [SkippableTheory(typeof(RequiredToolFeatureMissingException))]
        [MemberData(nameof(ShaderSets))]
        public void Glsl330Compile(string vsName, string fsName) => TestCompile(GraphicsBackend.OpenGL, vsName, fsName);

        [SkippableTheory(typeof(RequiredToolFeatureMissingException))]
        [MemberData(nameof(ShaderSets))]
        public void GlslEs300Compile(string vsName, string fsName) => TestCompile(GraphicsBackend.OpenGLES, vsName, fsName);

        [SkippableTheory(typeof(RequiredToolFeatureMissingException))]
        [MemberData(nameof(ShaderSets))]
        public void Glsl450Compile(string vsName, string fsName) => TestCompile(GraphicsBackend.Vulkan, vsName, fsName);

        [SkippableTheory(typeof(RequiredToolFeatureMissingException))]
        [MemberData(nameof(ShaderSets))]
        public void MetalCompile(string vsName, string fsName) => TestCompile(GraphicsBackend.Metal, vsName, fsName);

        [SkippableFact(typeof(RequiredToolFeatureMissingException))]
        public void AllSetsCompile()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();

            // Get all available tool chains.
            LanguageBackend[] backends = ToolChain.Require(ToolFeatures.ToCompiled, false)
                .Select(t => t.CreateBackend(compilation))
                .ToArray();

            ShaderGenerator sg = new ShaderGenerator(compilation, backends);
            ShaderGenerationResult generationResult = sg.GenerateShaders();

            string spacer1 = new string('=', 80);
            string spacer2 = new string('-', 80);

            bool failed = false;
            foreach (LanguageBackend backend in backends)
            {
                ToolChain toolChain = ToolChain.Get(backend);
                IReadOnlyList<GeneratedShaderSet> sets = generationResult.GetOutput(backend);
                _output.WriteLine(spacer1);
                _output.WriteLine($"Generated shader sets for {toolChain.Name} backend.");

                foreach (GeneratedShaderSet set in sets)
                {
                    _output.WriteLine(string.Empty);
                    _output.WriteLine(spacer2);
                    _output.WriteLine(string.Empty);
                    CompileResult result;

                    if (set.VertexShaderCode != null)
                    {
                        result = toolChain.Compile(set.VertexShaderCode, Stage.Vertex, set.VertexFunction.Name);
                        if (result.HasError)
                        {
                            _output.WriteLine($"Failed to compile Vertex Shader from set \"{set.Name}\"!");
                            _output.WriteLine(result.ToString());
                            failed = true;
                        }
                        else
                            _output.WriteLine($"Compiled Vertex Shader from set \"{set.Name}\"!");
                    }

                    if (set.FragmentFunction != null)
                    {
                        result = toolChain.Compile(set.FragmentShaderCode, Stage.Fragment, set.FragmentFunction.Name);
                        if (result.HasError)
                        {
                            _output.WriteLine($"Failed to compile Fragment Shader from set \"{set.Name}\"!");
                            _output.WriteLine(result.ToString());
                            failed = true;
                        }
                        else
                            _output.WriteLine($"Compiled Fragment Shader from set \"{set.Name}\"!");
                    }

                    if (set.ComputeFunction != null)
                    {
                        // TODO The skipped shaders are not included in the auto discovered shaders, leaving this here for completeness.
                        if (backend is GlslEs300Backend)
                        {
                            string fullname = set.ComputeFunction.DeclaringType + "." + set.ComputeFunction.Name + "_";
                            if (s_glslesSkippedShaders.Contains(fullname))
                                continue;
                        }

                        result = toolChain.Compile(set.ComputeShaderCode, Stage.Compute, set.ComputeFunction.Name);
                        if (result.HasError)
                        {
                            _output.WriteLine($"Failed to compile Compute Shader from set \"{set.Name}\"!");
                            _output.WriteLine(result.ToString());
                            failed = true;
                        }
                        else
                            _output.WriteLine($"Compiled Compute Shader from set \"{set.Name}\"!");
                    }
                }

                _output.WriteLine(string.Empty);
            }

            Assert.False(failed);
        }

        public static IEnumerable<object[]> ErrorSets()
        {
            yield return new object[] { "TestShaders.MissingFunctionAttribute.VS", null };
            yield return new object[] { "TestShaders.PercentOperator.PercentVS", null };
            yield return new object[] { "TestShaders.PercentOperator.PercentEqualsVS", null };
        }

        [Theory]
        [MemberData(nameof(ErrorSets))]
        public void ExpectedException(string vsName, string fsName)
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            Glsl330Backend backend = new Glsl330Backend(compilation);
            ShaderGenerator sg = new ShaderGenerator(compilation, backend, vsName, fsName);

            Assert.Throws<ShaderGenerationException>(() => sg.GenerateShaders());
        }
    }
}
