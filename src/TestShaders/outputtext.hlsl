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

struct TestShaders_VertexOutput

{
    float4 Position;
    float2 TextureCoord;
};

struct TestShaders_PositionTexture

{
    float3 Position;
    float2 TextureCoord;
};

TestShaders_VertexOutput VS(TestShaders_PositionTexture input)
{
TestShaders_VertexOutput output ;
float4 worldPosition = Mul(World, Vector4(input.Position, 1));
float4 viewPosition = Mul(View, worldPosition);
            output.Position = Mul(Projection, viewPosition);
            output.TextureCoord = input.TextureCoord;
return output;
}
