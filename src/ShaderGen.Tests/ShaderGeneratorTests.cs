using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
using ShaderGen.Metal;
using Xunit;

namespace ShaderGen.Tests
{
    public class ShaderGeneratorTests
    {
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
        }

        public static IEnumerable<object[]> ComputeShaders()
        {
            yield return new object[] { "TestShaders.SimpleCompute.CS" };
        }

        [Theory]
        [MemberData(nameof(ShaderSets))]
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
        [MemberData(nameof(ShaderSets))]
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
        [MemberData(nameof(ShaderSets))]
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

        [Theory]
        [MemberData(nameof(ShaderSets))]
        public void MetalEndToEnd(string vsName, string fsName)
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            LanguageBackend backend = new MetalBackend(compilation);
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
                MetalTool.AssertCompilesCode(vsCode);
            }
            if (fsName != null)
            {
                ShaderFunction fsFunction = shaderModel.GetFunction(fsName);
                string fsCode = set.FragmentShaderCode;
                MetalTool.AssertCompilesCode(fsCode);
            }
        }

        [Fact]
        public void AllSetsAllLanguagesEndToEnd()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            LanguageBackend[] backends = new LanguageBackend[]
            {
                new HlslBackend(compilation),
                new Glsl330Backend(compilation),
                new Glsl450Backend(compilation),
                new MetalBackend(compilation),
            };
            ShaderGenerator sg = new ShaderGenerator(compilation, backends);

            ShaderGenerationResult result = sg.GenerateShaders();

            foreach (LanguageBackend backend in backends)
            {
                IReadOnlyList<GeneratedShaderSet> sets = result.GetOutput(backend);
                foreach (GeneratedShaderSet set in sets)
                {
                    if (set.VertexShaderCode != null)
                    {
                        if (backend is HlslBackend)
                        {
                            FxcTool.AssertCompilesCode(set.VertexShaderCode, "vs_5_0", set.VertexFunction.Name);
                        }
                        else if (backend is MetalBackend)
                        {
                            MetalTool.AssertCompilesCode(set.VertexShaderCode);
                        }
                        else
                        {
                            bool is450 = backend is Glsl450Backend;
                            GlsLangValidatorTool.AssertCompilesCode(set.VertexShaderCode, "vert", is450);
                        }
                    }
                    if (set.FragmentFunction != null)
                    {
                        if (backend is HlslBackend)
                        {
                            FxcTool.AssertCompilesCode(set.FragmentShaderCode, "ps_5_0", set.FragmentFunction.Name);
                        }
                        else if (backend is MetalBackend)
                        {
                            MetalTool.AssertCompilesCode(set.FragmentShaderCode);
                        }
                        else
                        {
                            bool is450 = backend is Glsl450Backend;
                            GlsLangValidatorTool.AssertCompilesCode(set.FragmentShaderCode, "frag", is450);
                        }
                    }
                    if (set.ComputeFunction != null)
                    {
                        if (backend is HlslBackend)
                        {
                            FxcTool.AssertCompilesCode(set.ComputeShaderCode, "cs_5_0", set.ComputeFunction.Name);
                        }
                        else if (backend is MetalBackend)
                        {
                            MetalTool.AssertCompilesCode(set.ComputeShaderCode);
                        }
                        else
                        {
                            bool is450 = backend is Glsl450Backend;
                            GlsLangValidatorTool.AssertCompilesCode(set.ComputeShaderCode, "comp", is450);
                        }
                    }
                }
            }
        }

        public static IEnumerable<object[]> ErrorSets()
        {
            yield return new object[] { "TestShaders.MissingFunctionAttribute.VS", null };
            yield return new object[] { "TestShaders.PercentOperator.PercentVS", null };
            yield return new object[] { "TestShaders.PercentOperator.PercentEqualsVS", null };
        }

        [Theory]
        [MemberData(nameof(ErrorSets))]
        public void ExceptedException(string vsName, string fsName)
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            Glsl330Backend backend = new Glsl330Backend(compilation);
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                vsName,
                fsName,
                backend);

            Assert.Throws<ShaderGenerationException>(() => sg.GenerateShaders());
        }
    }
}
