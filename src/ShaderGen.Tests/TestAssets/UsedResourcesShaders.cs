using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    public class UsedResourcesShaders
    {
        public Matrix4x4 VS_M0;
        public Matrix4x4 VS_M1;
        public Texture2DResource VS_T0;
        public SamplerResource VS_S0;

        public Matrix4x4 FS_M0;
        public Matrix4x4 FS_M1;
        public Texture2DResource FS_T0;
        public SamplerResource FS_S0;
        public Matrix4x4 FS_M2_Indirect;

        [VertexShader]
        SystemPosition4 VS(Position4 input)
        {
            Vector2 v2 = new Vector2(VS_M0.M11, VS_M1.M22);
            Vector4 v4 = Sample(VS_T0, VS_S0, v2);

            SystemPosition4 output;
            output.Position = v4;
            return output;
        }

        [FragmentShader]
        Vector4 FS(SystemPosition4 input)
        {
            Vector2 v2 = new Vector2(FS_M0.M11, FS_M1.M22);
            v2.X += GetIndirectOffset();
            return Sample(FS_T0, FS_S0, v2);
        }

        private float GetIndirectOffset()
        {
            return FS_M2_Indirect.M11;
        }
    }
}
