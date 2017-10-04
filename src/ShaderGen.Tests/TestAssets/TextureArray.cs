using ShaderGen;
using static ShaderGen.ShaderBuiltins;
using System.Numerics;

namespace TestShaders
{
    public class TextureArray
    {
        [ArraySize(4)] public Texture2DResource[] TexArray;
        public SamplerResource Sampler;

        [VertexShader]
        Position4Texture2 VS(Position4Texture2 input)
        {
            return input;
        }

        [FragmentShader]
        Vector4 FS(Position4Texture2 input)
        {
            if (input.Position.Y > 0)
                return Sample(TexArray[0], Sampler, input.TextureCoord);
            if (input.Position.X < 0)
                return Sample(TexArray[1], Sampler, input.TextureCoord);
            if (input.Position.X == 0)
                return Sample(TexArray[2], Sampler, input.TextureCoord);
            else
                return Sample(TexArray[3], Sampler, input.TextureCoord);
        }
    }
}
