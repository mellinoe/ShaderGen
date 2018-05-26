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
                ShaderGenerator sg = new ShaderGenerator(compilation, backend, "ShaderGen.Tests.ReferenceTypeField.VS");

                Assert.Throws<ShaderGenerationException>(() => sg.GenerateShaders());
            }
        }
    }

    class ReferenceTypeField
    {
#pragma warning disable 0649
        public object ReferenceField;
#pragma warning restore 0649

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
