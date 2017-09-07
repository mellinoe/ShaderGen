cbuffer WorldBuffer : register(b0)
{
    float4x4 World;
}

cbuffer ViewBuffer : register(b1)
{
    float4x4 View;
}

cbuffer ProjectionBuffer : register(b2)
{
    float4x4 Projection;
}

struct VertexOutput
{
    float4 Position;
    float2 TextureCoord;
};

struct PositionTexture
{
    float3 Position;
    float2 TextureCoord;
};

Encountered method declaration: VS
  - Is a shader method.
  * Assignment: [[             output.Position = Vector4.Transform(viewPosition, Projection) ]]
  * Assignment: [[             output.TextureCoord = input.TextureCoord ]]
