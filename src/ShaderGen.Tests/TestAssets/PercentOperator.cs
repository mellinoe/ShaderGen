using ShaderGen;

namespace TestShaders
{
    public class PercentOperator
    {
        [VertexShader]
        public SystemPosition4 PercentEqualsVS(Position4 input)
        {
            float x = 5;
            x %= input.Position.Y;
            SystemPosition4 output;
            output.Position = input.Position;
            return output;
        }
    }
}
