using System.Numerics;
using ShaderGen;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    class Swizzles
    {
        [VertexShader]
        Position4Texture2 VS(Position4Texture2 input)
        {
            input.Position = input.Position.WZYX();
            input.Position = input.Position.WWXY();
            input.TextureCoord = input.TextureCoord.YY();
            return input;
        }
    }
}
