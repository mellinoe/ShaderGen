using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class PointLightTestShaders
    {
        public PointLightsInfo PointLights;

        [VertexShader] SystemPosition4 VS(Position4 input)
        {
            SystemPosition4 output;
            output.Position = input.Position;
            return output;
        }
    }

    public struct PointLightInfo
    {
        public Vector3 Position;
        public float Range;
        public Vector3 Color;
        public float _padding;
    }

    public struct PointLightsInfo
    {
        public int NumActiveLights;
        public Vector3 _padding;
        [ArraySize(4)] public PointLightInfo[] PointLights;
    }
}
