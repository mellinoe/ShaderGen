struct TargetProject_VertexAndFragment_VertexInput
{
    float3 Position : POSITION0;
};

struct TargetProject_VertexAndFragment_FragmentInput
{
    float4 Position : POSITION0;
};

struct TargetProject_VertexAndFragment_FragmentInput__FRAGSEMANTICS
{
    float4 Position : SV_POSITION;
};

float4 FS(TargetProject_VertexAndFragment_FragmentInput__FRAGSEMANTICS input) : SV_Target
{
    return float4(input.Position.x, input.Position.y, input.Position.z, 1);
}


