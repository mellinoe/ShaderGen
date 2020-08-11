using ShaderGen;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestShaders
{
    public class PointLightTestShaders
    {
        public PointLightsInfo PointLights;
        public const int MyOtherConst = 20;

        [VertexShader]
        public SystemPosition4 VS(Position4 input)
        {
            const int MyConst = 10;

            for (int i = 0; i < MyConst; i++) { }
            for (int i = 0; i < MyOtherConst; i++) { }

            SystemPosition4 output;
            Vector4 color = Vector4.Zero;
            for (int i = 0; i < PointLightsInfo.MaxLights; i++)
            {
                PointLightInfo a = PointLights.PointLights[i];
                color += new Vector4(a.Color, MyConst);
            }
            output.Position = input.Position;
            return output;
        }
    }

    public struct PointLightInfo
    {
        public Vector3 Position;
        public float Range;
        public Vector3 Color;
        public PointLightType LightType;
    }

    public enum PointLightType
    {
        Lambert,
        Phong
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PointLightsInfo
    {
        public const int MaxLights = 4;

        public int NumActiveLights;
        public float _padding0;
        public float _padding1;
        public float _padding2;
        [ArraySize(MaxLights)] public PointLightInfo[] PointLights;
        [ArraySize(2)] public PointLightInfo[] PointLights2;
    }
}
