float4x4 View;
float4x4 Projection;

float FarPlane;

struct VertexShaderInput
{
    float3 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float3 ViewPos : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = float4(input.Position.xyz, 1);
    
    float4 viewPosition = mul(worldPosition, View);
    
    output.Position = mul(viewPosition, Projection);

    output.ViewPos = viewPosition.xyz;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    return float4(length(input.ViewPos) / FarPlane, 0, 0, 1);
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
