samplerCUBE SkySampler : register(s0);

float4x4 ViewProjection;

void VS(in float4 position : POSITION0, out float4 pos : POSITION0, out float3 texCoord : TEXCOORD0)
{
    pos = mul(float4(position.xyz, 1), ViewProjection).xyww;
		
	texCoord = position.xyz;
}

float4 PS(in float3 pos : TEXCOORD0) : COLOR0
{
	float3 sample = texCUBE(SkySampler, normalize(pos));
	
    return float4(sample, 1);
}

technique Sky
{
  pass pass1
  {
     VertexShader = compile vs_2_0 VS();
	 PixelShader = compile ps_2_0 PS();
  }
}