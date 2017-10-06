using ShaderGen;

namespace TestShaders
{
    public class PercentOperator
    {
        //[VertexShader]
        //public Position4 PercentVS(Position4 input)
        //{
        //    float x = input.Position.X % 10;
        //    return input;
        //}

        [VertexShader]
        public Position4 PercentEqualsVS(Position4 input)
        {
            float x = 5;
            x %= input.Position.Y;
            return input;
        }
    }
}
