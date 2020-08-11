using Microsoft.CodeAnalysis;
using TestShaders;
using Xunit;

namespace ShaderGen.Tests
{
    public static class ShaderResourceTests
    {
        [Fact]
        public static void ReferenceTypeField_ThrowsShaderGenerationException()
        {
            Compilation compilation = TestUtil.GetCompilation();
            foreach (LanguageBackend backend in TestUtil.GetAllBackends(compilation))
            {
                ShaderGenerator sg = new ShaderGenerator(compilation, backend, "TestShaders.ReferenceTypeField.VS");

                Assert.Throws<ShaderGenerationException>(() => sg.GenerateShaders());
            }
        }
    }

 
}
