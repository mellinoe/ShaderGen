using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class PointLightTestShaders
    {
        public PointLightsInfo PointLights;

        [VertexShader] Position4 VS(Position4 input)
        {
            return input;
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
