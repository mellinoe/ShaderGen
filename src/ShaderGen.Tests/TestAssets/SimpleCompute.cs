using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    public class SimpleCompute
    {
        public StructuredBuffer<Vector4> StructuredInput;
        public RWStructuredBuffer<Vector4> StructuredInOut;

        public RWStructuredBuffer<PointLightInfo> RWBufferWithCustomStruct;

        [ComputeShader(1, 1, 1)]
        public void CS()
        {
            StructuredInOut[DispatchThreadID.X] = StructuredInput[DispatchThreadID.Y];
            StructuredInOut[DispatchThreadID.Y].Z = 1;
            StructuredInOut[DispatchThreadID.Z] = GetValue(StructuredInput);

            RWBufferWithCustomStruct[0].Color = new Vector3(1, 2, 3);
        }

        private Vector4 GetValue(StructuredBuffer<Vector4> myBuffer)
        {
            return myBuffer[0];
        }
    }
}
