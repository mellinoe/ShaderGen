using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    internal class MultipleResourceSets
    {
#pragma warning disable 0649
        public Matrix4x4 NoAttributeMatrix;
        [ResourceSet(0)] public Matrix4x4 Matrix0;
        [ResourceSet(1)] public Matrix4x4 Matrix1;
        [ResourceSet(2)] public Matrix4x4 Matrix2;
        [ResourceSet(3)] public Matrix4x4 Matrix4;
        [ResourceSet(4)] public Matrix4x4 Matrix3;
        [ResourceSet(0)] public Matrix4x4 Matrix00;

        [ResourceSet(0)] public SamplerResource Sampler0;
        [ResourceSet(4)] public SamplerResource Sampler4;
        public SamplerResource NoAttributeSampler;

        [ResourceSet(2)] public Texture2DResource Texture2D2;
        public Texture2DResource NoAttributeTexture2D;
        [ResourceSet(1)] public Texture2DResource Texture2D1;
#pragma warning restore 0649

        [VertexShader]
        public Position4 VS(Position4 input)
        {
            Position4 output;
            Matrix4x4 result = NoAttributeMatrix * Matrix0 * Matrix1 * Matrix2 * Matrix3 * Matrix4 * Matrix00;
            output.Position = Mul(result, input.Position);
            return output;
        }

        [FragmentShader]
        public Vector4 FS(Position4 input)
        {
            return input.Position;
        }
    }
}
