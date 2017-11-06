using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class CustomMethodCalls
    {
        [VertexShader]
        SystemPosition4 VS(Position4 input)
        {
            Position4 reversed = Reverse(input);
            Position4 shuffled = ShufflePosition4(reversed);
            SystemPosition4 output;
            output.Position = shuffled.Position;
            return output;
        }

        private Position4 Reverse(Position4 vert)
        {
            vert.Position = vert.Position.WZYX();
            return vert;
        }

        private Position4 ShufflePosition4(Position4 vert)
        {
            vert.Position = ShuffleVector4(vert.Position);
            return vert;
        }

        private Vector4 ShuffleVector4(Vector4 v)
        {
            Vector4 result = v.XZYW();
            return result;
        }
    }
}
