using ShaderGen;
using System.Numerics;
using static ShaderGen.Builtins;

namespace TestShaders
{
    internal class MultipleResourceSets
    {
#pragma warning disable 0649
        public Matrix4x4 NoAttributeMatrix;                     // 0
        [ResourceSet(0)] public Matrix4x4 Matrix0;              // 1
        [ResourceSet(1)] public Matrix4x4 Matrix1;              // 2
        [ResourceSet(2)] public Matrix4x4 Matrix2;              // 3
        [ResourceSet(3)] public Matrix4x4 Matrix4;              // 4
        [ResourceSet(4)] public Matrix4x4 Matrix3;              // 5
        [ResourceSet(0)] public Matrix4x4 Matrix00;             // 6

        [ResourceSet(0)] public SamplerResource Sampler0;       // 7
        [ResourceSet(4)] public SamplerResource Sampler4;       // 8
        public SamplerResource NoAttributeSampler;              // 9

        [ResourceSet(2)] public Texture2DResource Texture2D2;   // 10
        public Texture2DResource NoAttributeTexture2D;          // 11
        [ResourceSet(1)] public Texture2DResource Texture2D1;   // 12
#pragma warning restore 0649

        [VertexShader]
        public SystemPosition4 VS(Position4 input)
        {
            Vector4 outputPos;
            Matrix4x4 result = NoAttributeMatrix * Matrix0 * Matrix1 * Matrix2 * Matrix3 * Matrix4 * Matrix00;
            outputPos = Mul(result, input.Position);

            SystemPosition4 output;
            output.Position = outputPos;
            return output;
        }

        [FragmentShader]
        public Vector4 FS(SystemPosition4 input)
        {
            return input.Position;
        }
    }
}
