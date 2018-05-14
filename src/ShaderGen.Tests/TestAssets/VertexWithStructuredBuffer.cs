using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class VertexWithStructuredBuffer
    {
        public RWStructuredBuffer<Vector4> Vectors;

        [VertexShader]
        public SystemPosition4 VS()
        {
            SystemPosition4 output;
            output.Position = Vectors[ShaderBuiltins.VertexID];
            return output;
        }
    }
}
