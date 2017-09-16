using ShaderGen;
using System.Numerics;

namespace TargetProject
{
    public class VertexAndFragment
    {
        [VertexShader]
        public FragmentInput VS(VertexInput input)
        {
            FragmentInput output;
            output.Position = new Vector4(input.Position, 1);
            return output;
        }

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            return new Vector4(input.Position.X, input.Position.Y, input.Position.Z, 1);
        }

        public struct VertexInput
        {
            [VertexSemantic(SemanticType.Position)]
            public Vector3 Position;
        }

        public struct FragmentInput
        {
            [VertexSemantic(SemanticType.Position)]
            public Vector4 Position;
        }
    }
}
