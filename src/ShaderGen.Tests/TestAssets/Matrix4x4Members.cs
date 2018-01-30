using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    internal class Matrix4x4Members
    {
        public Matrix4x4 InputMatrix;

        [VertexShader]
        public SystemPosition4 VS(Position4 input)
        {
            Matrix4x4 newMat = new Matrix4x4(
                InputMatrix.M11, InputMatrix.M12, InputMatrix.M13, 0,
                InputMatrix.M21, InputMatrix.M22, InputMatrix.M23, 0,
                InputMatrix.M31, InputMatrix.M32, InputMatrix.M33, 0,
                0, 0, 0, 1);

            SystemPosition4 output;
            output.Position = Vector4.Transform(input.Position, newMat);
            return output;
        }
    }
}
