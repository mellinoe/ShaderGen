using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class SwitchStatements
    {
        public int Something;

        [VertexShader]
        public VertexOutput VS(PositionTexture input)
        {
            float x;
            switch (Something)
            {
                case 0:
                    x = -5;
                    break;

                case 1:
                {
                    x = 3;
                    break;
                }

                default:
                    x = 0;
                    break;
            }

            VertexOutput output;
            output.Position = new Vector4(x, 1, 1, 1);
            return output;
        }

        public struct VertexOutput
        {
            [VertexSemantic(SemanticType.SystemPosition)]
            public Vector4 Position;
        }
    }
}
