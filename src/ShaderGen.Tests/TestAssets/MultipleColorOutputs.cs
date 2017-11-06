using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class MultipleColorOutputs
    {
        [VertexShader]
        public SystemPosition4 VS(Position4 input)
        {
            SystemPosition4 output;
            output.Position = input.Position;
            return output;
        }

        [FragmentShader]
        public DualOutput FS(SystemPosition4 input)
        {
            DualOutput output;
            output.FirstOutput = new Vector4(input.Position.X, input.Position.Y, input.Position.Z, 1);
            output.SecondOutput = new Vector4(input.Position.Z, input.Position.X, input.Position.Y, 1);
            return output;
        }

        public struct DualOutput
        {
            [ColorTargetSemantic]
            public Vector4 FirstOutput;
            [ColorTargetSemantic]
            public Vector4 SecondOutput;
        }
    }
}
