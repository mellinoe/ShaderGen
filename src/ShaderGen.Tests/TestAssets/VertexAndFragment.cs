using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class VertexAndFragment
    {
        [VertexShader]
        public FragmentInput VS(VertexInput input)
        {
            FragmentInput output;
            output.Position = new Vector4(input.Position, 1);
            output.Position.X += input.VertexID;
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

            [VertexSemantic(SemanticType.SystemVertexId)]
            public uint VertexID;
        }

        public struct FragmentInput
        {
            [VertexSemantic(SemanticType.SystemPosition)]
            public Vector4 Position;
        }
    }
}
