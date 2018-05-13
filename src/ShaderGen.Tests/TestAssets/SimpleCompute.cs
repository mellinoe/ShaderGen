using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    public class SimpleCompute
    {
        public StructuredBuffer<Matrix4x4> StructuredInput;
        public RWStructuredBuffer<Vector4> StructuredInOut;

        public RWStructuredBuffer<PointLightInfo> RWBufferWithCustomStruct;
        public RWTexture2DResource<float> RWTex;

        [ComputeShader(1, 1, 1)]
        public void CS()
        {
            Matrix4x4 m = StructuredInput[DispatchThreadID.Y];
            StructuredInOut[DispatchThreadID.X].X = m.M11;
            StructuredInOut[DispatchThreadID.Y].Z = 1;

            RWBufferWithCustomStruct[0].Color = new Vector3(1, 2, 3);

            float existing = Load(RWTex, new UInt2(10, 20));
            Store(
                RWTex,
                new UInt2(10, 20),
                existing + DispatchThreadID.X);
        }
    }
}
