using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class CustomMethodUsingUniform
    {
        public Vector4 UniformV4;

        [VertexShader]
        SystemPosition4 VS(Position4 input)
        {
            return CustomMethod(input);
        }

        private SystemPosition4 CustomMethod(Position4 vert)
        {
            SystemPosition4 ret;
            ret.Position = vert.Position + UniformV4;
            return ret;
        }
    }
}
