using ShaderGen;
using System.Numerics;

[assembly:ShaderSet("SimpleSet", "TargetProject.VertexAndFragment.VS", "TargetProject.VertexAndFragment.FS")]

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
            Vector4 result = new Vector4(input.Position.X, input.Position.Y, input.Position.Z, 1);
            Vector3 swizzleVector = result.XYZ();
            return result.XYZZ();
        }

        public struct VertexInput
        {
            [VertexSemantic(SemanticType.Position)]
            public Vector3 Position;
        }

        public struct FragmentInput
        {
            [SystemPositionSemanticAttribute]
            public Vector4 Position;
        }
    }
}
