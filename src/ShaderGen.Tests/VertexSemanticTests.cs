﻿using Microsoft.CodeAnalysis;
using Xunit;

namespace ShaderGen.Tests
{
    public class VertexSemanticTests
    {
        [Fact]
        public void MissingSemantic_ThrowsShaderGenerationException()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            foreach (LanguageBackend backend in TestUtil.GetAllBackends(compilation))
            {
                ShaderGenerator sg = new ShaderGenerator(
                    compilation,
                    "TestShaders.MissingInputSemantics.VS",
                    null,
                    backend);

                Assert.Throws<ShaderGenerationException>(() => sg.GenerateShaders());
            }
        }
    }
}
