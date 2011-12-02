struct VertexShaderInput
{
    float3 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = float4(input.Position, 1);
	output.TexCoord = input.TexCoord;
	
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 mirroredCoords = abs(2 * input.TexCoord - 1);
    
    float sinX = sin(mirroredCoords.x * 0.5 + 0.5f);
    float sinY = cos(mirroredCoords.y * 10);
    
    return float4(1 - (sinX + sinY), 0, 0,1);
}

technique GenerateHeight
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

sampler2D HeightSampler : register(s0);

float4 DUDVFunction(VertexShaderOutput input) : COLOR0
{        
	float val = tex2D(HeightSampler, input.TexCoord).r;
    return float4(ddx(val), ddy(val), 0,1);
}

technique HeightToDUDV
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 DUDVFunction();
    }
}
