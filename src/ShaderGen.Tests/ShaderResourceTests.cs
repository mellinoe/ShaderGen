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
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            foreach (LanguageBackend backend in TestUtil.GetAllBackends(compilation))
            {
                ShaderGenerator sg = new ShaderGenerator(
                    compilation,
                    "ShaderGen.Tests.ReferenceTypeField.VS",
                    null,
                    backend);

                Assert.Throws<ShaderGenerationException>(() => sg.GenerateShaders());
            }
        }
    }

    class ReferenceTypeField
    {
        public object ReferenceField;

        [VertexShader]
        public Position4Texture2 VS(PositionTexture input)
        {
            Position4Texture2 output;
            output.Position = new System.Numerics.Vector4(input.Position, 1);
            output.TextureCoord = input.TextureCoord;
            return output;
        }
    }
}
