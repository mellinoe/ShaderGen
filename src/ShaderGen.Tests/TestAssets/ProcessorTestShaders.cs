using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class ProcessorTestShaders
    {
#pragma warning disable 0649
        public Matrix4x4 This;
        public Texture2DResource Sentence;
        public SamplerResource Should;
        public TextureCubeResource Be;
        public Vector4 Printed;
        public CustomBlittableStruct By_Enumerating;
        public Vector4 All;
        public Matrix4x4 Resources;
        public SamplerResource In;
        public Matrix4x4 Order;
#pragma warning restore 0649

        [VertexShader]
        SystemPosition4 VS(Position4 input)
        {
            SystemPosition4 output;
            output.Position = input.Position;
            return output;
        }

        [FragmentShader]
        Vector4 FS(SystemPosition4 input) { return input.Position; }
    }
}
