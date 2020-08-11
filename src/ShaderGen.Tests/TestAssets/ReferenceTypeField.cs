using ShaderGen;

namespace TestShaders
{
    public class ReferenceTypeField
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