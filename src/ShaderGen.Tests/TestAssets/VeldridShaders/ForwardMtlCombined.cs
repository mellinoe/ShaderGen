using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;
using System;

namespace TestShaders.VeldridShaders
{
    public class ForwardMtlCombined
    {
        public Matrix4x4 Projection;
        public Matrix4x4 View;
        public Matrix4x4 World;
        public Matrix4x4 InverseTransposeWorld;
        public Matrix4x4 LightProjection;
        public Matrix4x4 LightView;
        public LightInfoBuffer LightInfo;
        public CameraInfoBuffer CameraInfo;
        public PointLightsBuffer PointLights;
        public MaterialPropertiesBuffer MaterialProperties;
        public Texture2DResource SurfaceTexture;
        public SamplerResource RegularSampler;
        public Texture2DResource AlphaMap;
        public SamplerResource AlphaMapSampler;
        public Texture2DResource ShadowMap;
        public SamplerResource ShadowMapSampler;

        public struct LightInfoBuffer
        {
            public Vector3 LightDir;
            public float __padding;
        }

        public struct CameraInfoBuffer
        {
            public Vector3 CameraPosition_WorldSpace;
            public float __padding1;
            public Vector3 CameraLookDirection;
            public float __padding2;
        }

        public struct PointLightInfo
        {
            public Vector3 Position;
            public float Range;
            public Vector3 Color;
            public float __padding;
        }

        public struct PointLightsBuffer
        {
            public int NumActiveLights;
            public Vector3 __padding;
            [ArraySize(4)] public PointLightInfo[] PointLights;
        }

        public struct MaterialPropertiesBuffer
        {
            public Vector3 SpecularIntensity;
            public float SpecularPower;
        }

        public struct VertexInput
        {
            [VertexSemantic(SemanticType.Position)] public Vector3 Position;
            [VertexSemantic(SemanticType.Normal)] public Vector3 Normal;
            [VertexSemantic(SemanticType.TextureCoordinate)] public Vector2 TexCoord;
        }

        public struct PixelInput
        {
            [VertexSemantic(SemanticType.Position)] public Vector4 Position;
            [VertexSemantic(SemanticType.Position)] public Vector3 Position_WorldSpace;
            [VertexSemantic(SemanticType.TextureCoordinate)] public Vector4 LightPosition;
            [VertexSemantic(SemanticType.Normal)] public Vector3 Normal;
            [VertexSemantic(SemanticType.TextureCoordinate)] public Vector2 TexCoord;
        }

        [VertexShader]
        public PixelInput VS(VertexInput input)
        {
            PixelInput output;
            Vector4 worldPosition = Mul(World, new Vector4(input.Position, 1));
            Vector4 viewPosition = Mul(View, worldPosition);
            output.Position = Mul(Projection, viewPosition);

            output.Position_WorldSpace = new Vector3(worldPosition.X, worldPosition.Y, worldPosition.Z);

            Vector4 outNormal = Mul(InverseTransposeWorld, new Vector4(input.Normal, 1));
            output.Normal = Vector3.Normalize(new Vector3(outNormal.X, outNormal.Y, outNormal.Z));

            output.TexCoord = input.TexCoord;

            //store worldspace projected to light clip space with
            //a texcoord semantic to be interpolated across the surface
            output.LightPosition = Mul(World, new Vector4(input.Position, 1));
            output.LightPosition = Mul(LightView, output.LightPosition);
            output.LightPosition = Mul(LightProjection, output.LightPosition);

            return output;
        }

        [FragmentShader]
        public Vector4 FS(PixelInput input)
        {
            float alphaMapSample = Sample(AlphaMap, AlphaMapSampler, input.TexCoord).X;
            if (alphaMapSample == 0)
            {
                Discard();
            }

            Vector4 surfaceColor = Sample(SurfaceTexture, RegularSampler, input.TexCoord);
            Vector4 ambientLight = new Vector4(.4f, .4f, .4f, 1f);

            // Point Diffuse

            Vector4 pointDiffuse = new Vector4(0, 0, 0, 1);
            Vector4 pointSpec = new Vector4(0, 0, 0, 1);
            for (int i = 0; i < PointLights.NumActiveLights; i++)
            {
                PointLightInfo pli = PointLights.PointLights[i];
                Vector3 lightDir = Vector3.Normalize(pli.Position - input.Position_WorldSpace);
                float intensity = Saturate(Vector3.Dot(input.Normal, lightDir));
                float lightDistance = Vector3.Distance(pli.Position, input.Position_WorldSpace);
                intensity = Saturate(intensity * (1 - (lightDistance / pli.Range)));

                pointDiffuse += intensity * new Vector4(pli.Color, 1) * surfaceColor;

                // Specular
                Vector3 vertexToEye0 = Vector3.Normalize(CameraInfo.CameraPosition_WorldSpace - input.Position_WorldSpace);
                Vector3 lightReflect0 = Vector3.Normalize(Vector3.Reflect(lightDir, input.Normal));

                float specularFactor0 = Vector3.Dot(vertexToEye0, lightReflect0);
                if (specularFactor0 > 0)
                {
                    specularFactor0 = Pow(Abs(specularFactor0), MaterialProperties.SpecularPower);
                    pointSpec += (1 - (lightDistance / pli.Range)) * (new Vector4(pli.Color * MaterialProperties.SpecularIntensity * specularFactor0, 1.0f));
                }
            }

            pointDiffuse = Saturate(pointDiffuse);
            pointSpec = Saturate(pointSpec);

            // Directional light calculations

            //re-homogenize position after interpolation
            input.LightPosition /= input.LightPosition.W;
            input.LightPosition.W = 1;

            // if position is not visible to the light - dont illuminate it
            // results in hard light frustum
            if (input.LightPosition.X < -1.0f || input.LightPosition.X > 1.0f ||
                input.LightPosition.Y < -1.0f || input.LightPosition.Y > 1.0f ||
                input.LightPosition.Z < 0.0f || input.LightPosition.Z > 1.0f)
            {
                return WithAlpha((ambientLight * surfaceColor) + pointDiffuse + pointSpec, surfaceColor.X);
            }

            // transform clip space coords to texture space coords (-1:1 to 0:1)
            input.LightPosition.X = input.LightPosition.X / 2 + 0.5f;
            input.LightPosition.Y = input.LightPosition.Y / -2 + 0.5f;

            Vector3 L = -1 * Vector3.Normalize(LightInfo.LightDir);
            float diffuseFactor = Vector3.Dot(Vector3.Normalize(input.Normal), L);

            float cosTheta = Clamp(diffuseFactor, 0, 1);
            float bias = 0.0005f * Tan(Acos(cosTheta));
            bias = Clamp(bias, 0, 0.01f);

            input.LightPosition.Z -= bias;

            //sample shadow map - point sampler
            float ShadowMapDepth = Sample(ShadowMap, ShadowMapSampler, new Vector2(input.LightPosition.X, input.LightPosition.Y)).X;

            //if clip space z value greater than shadow map value then pixel is in shadow
            if (ShadowMapDepth < input.LightPosition.Z)
            {
                return WithAlpha((ambientLight * surfaceColor) + pointDiffuse + pointSpec, surfaceColor.X);
            }

            //otherwise calculate ilumination at fragment
            diffuseFactor = Clamp(diffuseFactor, 0, 1);

            Vector4 specularColor = new Vector4(0, 0, 0, 0);

            Vector3 vertexToEye = Vector3.Normalize(CameraInfo.CameraPosition_WorldSpace - input.Position_WorldSpace);
            Vector3 lightReflect = Vector3.Normalize(Vector3.Reflect(LightInfo.LightDir, input.Normal));
            Vector3 lightColor = new Vector3(1, 1, 1);

            float specularFactor = Vector3.Dot(vertexToEye, lightReflect);
            if (specularFactor > 0)
            {
                specularFactor = Pow(Abs(specularFactor), MaterialProperties.SpecularPower);
                specularColor = new Vector4(lightColor * MaterialProperties.SpecularIntensity * specularFactor, 1.0f);
            }

            return WithAlpha(specularColor + (ambientLight * surfaceColor)
                + (diffuseFactor * surfaceColor) + pointDiffuse + pointSpec, surfaceColor.X);
        }

        Vector4 WithAlpha(Vector4 baseColor, float alpha)
        {
            return new Vector4(baseColor.XYZ(), alpha);
        }
    }
}
