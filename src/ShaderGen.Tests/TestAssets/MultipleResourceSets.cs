using ShaderGen;
using System.Numerics;

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
            return input;
        }

        [FragmentShader]
        public Vector4 FS(Position4 input)
        {
            return input.Position;
        }
    }
}
