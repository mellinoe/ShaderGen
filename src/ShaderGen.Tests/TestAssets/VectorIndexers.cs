using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class VectorIndexers
    {
        [VertexShader]
        SystemPosition4 VS(Position4 input)
        {
            Vector2 v2 = new Vector2();
            float f = v2.Item(0);

            Vector3 v3 = new Vector3();
            f = v3.Item(0);

            Vector4 v4 = new Vector4();
            f = v4.Item(0);

            SystemPosition4 output;
            output.Position = input.Position;
            return output;
        }
    }
}
