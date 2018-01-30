using System.Numerics;
using ShaderGen;

namespace TestShaders
{
    class Swizzles
    {
        [VertexShader]
        SystemPosition4Texture2 VS(Position4Texture2 input)
        {
            input.Position = input.Position.WZYX();
            input.Position = input.Position.WWXY();
            input.TextureCoord = input.TextureCoord.YY();

            input.Position = Vector4.Normalize(input.Position).XYZW();

            SystemPosition4Texture2 output;
            output.Position = input.Position;
            output.TextureCoord = input.TextureCoord;
            return output;
        }
    }
}
