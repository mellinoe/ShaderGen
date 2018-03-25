# ShaderGen

A proof-of-concept library which generates shader code from C#. Currently, the project can generate HLSL (D3D11), GLSL-330 (core-GL-compatible), GLSL-450 (Vulkan-compatible), and Metal shader code from a single shader source specified in C#.

## Shaders in C#

Writing shader code in C# could have quite a few benefits:

* Easily share type definitions between graphics code in C# and shaders.
  * For example, one could re-use the same structure to describe the input to a vertex shader, as well as to store the actual vertex data in your C# program.
  * Shader uniforms ("constant buffers") can be shared as well.
* Analysis done at code-generation time can be used to build extra metadata about the shaders, enabling reflection-like capabilities.
  * The full vertex input specification can be generated when doing the C# analysis for code generation.
  * The layouts and order for all global shader resources can be captured.
  * Validation can be performed to ensure, for example, uniforms are multiples of 16-bytes in size, etc.
* C# refactoring tools can be used.
* C# niceties like inheritance, composition, partial declarations, etc. can be leveraged for easier shader writing (speculative).

## Example Shader

**NOTE: The examples below are a bit out of date with regards to the current output of the library. This is still a decent approximation of what the output looks like, though.**

Here is an example vertex and fragment shader, written in C# with ShaderGen:

```C#
public class MinExample
{
    public Matrix4x4 Projection;
    public Matrix4x4 View;
    public Matrix4x4 World;
    public Texture2DResource SurfaceTexture;
    public SamplerResource Sampler;

    public struct VertexInput
    {
        [PositionSemantic] public Vector3 Position;
        [TextureCoordinateSemantic] public Vector2 TextureCoord;
    }

    public struct FragmentInput
    {
        [SystemPositionSemanticAttribute] public Vector4 Position;
        [TextureCoordinateSemantic] public Vector2 TextureCoord;
    }

    [VertexShader]
    public FragmentInput VertexShaderFunc(VertexInput input)
    {
        FragmentInput output;
        Vector4 worldPosition = Mul(World, new Vector4(input.Position, 1));
        Vector4 viewPosition = Mul(View, worldPosition);
        output.Position = Mul(Projection, viewPosition);
        output.TextureCoord = input.TextureCoord;
        return output;
    }

    [FragmentShader]
    public Vector4 FragmentShaderFunc(FragmentInput input)
    {
        return Sample(SurfaceTexture, Sampler, input.TextureCoord);
    }
}
```

Here is some representative output from the library (subject to change, etc.):

HLSL Vertex Shader
```HLSL
struct MinExample_VertexInput
{
    float3 Position : POSITION0;
    float2 TextureCoord : TEXCOORD0;
};

struct MinExample_FragmentInput
{
    float4 Position : POSITION0;
    float2 TextureCoord : TEXCOORD0;
};

struct MinExample_FragmentInput__FRAGSEMANTICS
{
    float4 Position : SV_POSITION;
    float2 TextureCoord : TEXCOORD0;
};

cbuffer ProjectionBuffer : register(b0)
{
    float4x4 Projection;
}

cbuffer ViewBuffer : register(b1)
{
    float4x4 View;
}

cbuffer WorldBuffer : register(b2)
{
    float4x4 World;
}

MinExample_FragmentInput__FRAGSEMANTICS VertexShaderFunc(MinExample_VertexInput input)
{
    MinExample_FragmentInput output;
    float4 worldPosition = mul(World, float4(input.Position, 1));
    float4 viewPosition = mul(View, worldPosition);
    output.Position = mul(Projection, viewPosition);
    output.TextureCoord = input.TextureCoord;
    return output;
}
```

HLSL Fragment Shader
```HLSL
struct MinExample_FragmentInput__FRAGSEMANTICS
{
    float4 Position : SV_POSITION;
    float2 TextureCoord : TEXCOORD0;
};

Texture2D SurfaceTexture : register(t0);
SamplerState Sampler : register(s0);

float4 FragmentShaderFunc(MinExample_FragmentInput__FRAGSEMANTICS input) : SV_Target
{
    return SurfaceTexture.Sample(Sampler, input.TextureCoord);
}
```

GLSL (450) Vertex Shader
```GLSL
#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable
struct MinExample_VertexInput
{
    vec3 Position;
    vec2 TextureCoord;
};

struct MinExample_FragmentInput
{
    vec4 Position;
    vec2 TextureCoord;
};

layout(binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};
layout(binding = 1) uniform ViewBuffer
{
    mat4 View;
};
layout(binding = 2) uniform WorldBuffer
{
    mat4 World;
};

MinExample_FragmentInput VertexShaderFunc(MinExample_VertexInput input_)
{
    MinExample_FragmentInput output_;
    vec4 worldPosition = World * vec4(input_.Position, 1);
    vec4 viewPosition = View * worldPosition;
    output_.Position = Projection * viewPosition;
    output_.TextureCoord = input_.TextureCoord;
    return output_;
}

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TextureCoord;
layout(location = 0) out vec2 out_TextureCoord;

void main()
{
    MinExample_VertexInput input_;
    input_.Position = Position;
    input_.TextureCoord = TextureCoord;
    MinExample_FragmentInput output_ = VertexShaderFunc(input_);
    out_TextureCoord = output_.TextureCoord;
    gl_Position = output_.Position;
}
```

GLSL (450) Fragment Shader
```GLSL
#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

struct MinExample_FragmentInput
{
    vec4 Position;
    vec2 TextureCoord;
};

layout(binding = 3) uniform texture2D SurfaceTexture;
layout(binding = 4) uniform sampler Sampler;

vec4 FragmentShaderFunc(MinExample_FragmentInput input_)
{
    return texture(sampler2D(SurfaceTexture, Sampler), input_.TextureCoord);
}

layout(location = 0) in vec4 Position;
layout(location = 1) in vec2 TextureCoord;
layout(location = 0) out vec4 _outputColor_;

void main()
{
    MinExample_FragmentInput input_;
    input_.Position = Position;
    input_.TextureCoord = TextureCoord;
    vec4 output_ = FragmentShaderFunc(input_);
    _outputColor_ = output_;
}
```
