using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class VariableTypes
    {
        public bool SkinningEnabled;

        [VertexShader]
        public VertexOutput VS(PositionTexture input)
        {
            VertexOutput output;
            if (SkinningEnabled)
            {
                output.Position = new Vector4(1, 1, 1, 1);
            }
            else
            {
                output.Position = new Vector4(0, 1, 1, 1);
            }
            float l = 0;
            uint l2 = (uint) l;
            return output;
        }

        public struct VertexOutput
        {
            [VertexSemantic(SemanticType.SystemPosition)]
            public Vector4 Position;
        }
    }
}
