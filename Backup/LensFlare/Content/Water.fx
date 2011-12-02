float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WaterViewProjection;

float time;

float3 CameraPosition;
float3 LightDirection;

float3 SurfaceColor = {0.36, 0.664, 0.608};
float3 DeepColor = {0.09, 0.166, 0.177};

const float DeepDepth = 50.0;

float FarPlane = 1000.0f - 0.1f;

struct VertexShaderInput
{
    float3 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position    : POSITION0;
    float2 TexCoord    : TEXCOORD0;
    float4 ClipPos     : TEXCOORD1;
    float4 ReflClipPos : TEXCOORD2;
    float3 ViewPos     : TEXCOORD3;    
    float3 WorldPos     : TEXCOORD4; 
};

sampler2D DepthSampler : register(s0);
sampler2D ReflectionSampler : register(s1);
sampler2D RefractionSampler : register(s2);
sampler2D OffsetSampler : register(s3);
sampler2D NormalSampler : register(s4);

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 objectPos = float4(input.Position, 1);
    float4 worldPosition = mul(objectPos, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
        
    const float2 scrollDirection = normalize(float2(1.0f, 0.5f));
    
    float time = time / 1000.0f;
	float2 scroll = scrollDirection * time * 10.1f;
	
    output.TexCoord  = input.TexCoord + scroll;
	output.ClipPos = output.Position;
	
	float4x4 waterWorldViewProjection = mul(World, WaterViewProjection);
    output.ReflClipPos = mul(objectPos, waterWorldViewProjection);
	
	output.ViewPos = viewPosition;
	output.WorldPos = worldPosition;
	
    return output;
}

float2 ClipSpaceToTexCoord(float4 input)
{
   return float2(0.5f, -0.5f) * (input.xy / input.w) + 0.5f;
}

float2 ClipSpaceToTexCoordPerturb(float4 input, float2 perturb)
{
   return float2(0.5f, -0.5f) * ((input.xy + perturb) / input.w) + 0.5f;
}

float fresnel(float3 light, float3 normal, float R0) 
{ 
    const float refractionIndexApprox = 0.11109;

    float cosAngle = 1-saturate(dot(light, normal));    
 
    float result = cosAngle * cosAngle; 
    result       = result * result;                
    result       = result * cosAngle;  
    result       = saturate(result * (1-saturate(R0)) + R0); 
     
    return result; 
}

float FresnelApproximation(float3 lightDir, float3 normal, float offset)
{	
	float3 reflectedViewDir = -reflect(lightDir, normal);
	
	float viewDotNorm = abs(dot(lightDir, normal));
	float fresnelFactor = 1 - pow(viewDotNorm, 0.5);
	
	return saturate(fresnelFactor + offset);
}

float PhongSpecular(float3 normal, float3 viewDir, float specularDecay)
{
	float nDotL = dot(normal, LightDirection);

	float3 reflection = 2.0f * normal * nDotL + LightDirection;
	
	reflection = normalize(reflection);
	
	float rdotV = saturate(dot(reflection, viewDir));

	return pow(rdotV, specularDecay);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 screenTexCoords = ClipSpaceToTexCoord(input.ClipPos);
	
	float2 dudv = 2 * tex2D(OffsetSampler, input.TexCoord * 8).rg - 1;
	
	float2 refractCoords = ClipSpaceToTexCoordPerturb(input.ClipPos, dudv);
	
	// Get the terrain depth
	float sceneViewZ = (tex2D(DepthSampler, screenTexCoords).r - 0.001f) * FarPlane;
	
	float viewLen = length(input.ViewPos);
	
	float3 normal = 2 * tex2D(NormalSampler, input.TexCoord * 8).rgb - 1;
	
	float3 normalWorld = -normalize((float3(0, 1, 0) * normal.z) + (normal.x * float3(0, 0, 1) + normal.y * float3(-1, 0, 0)));
	
	// Get camera direction from view matrix
	float3 viewDir = normalize(input.WorldPos - CameraPosition);
	
	// Get the reflection factor so we can use it in our lerp
	float reflectionFactor = FresnelApproximation(viewDir, normalWorld, 0.3f);
		
	// Get the range of depth from waterplane to terrain
	float depthRange = (sceneViewZ - viewLen);
	
	const float shoreFalloff = 2.0f;
	const float shoreScale = 5.0f;
	
	// calculate a transparency value using a power function
	float alpha = saturate(max(pow(depthRange / FarPlane, shoreFalloff) * FarPlane * shoreScale, 0));
	
	float3 refraction = tex2D(RefractionSampler, refractCoords).rgb;

	float3 waterColor = DeepColor * refraction;
	
	if (depthRange <= DeepDepth)
	{
		float difference = DeepDepth - depthRange;
		float factor = saturate(pow((difference / depthRange), shoreFalloff));
	
		waterColor = lerp(waterColor, SurfaceColor * refraction, factor);
	}
	
	float2 reflectTexCoords = ClipSpaceToTexCoordPerturb(input.ReflClipPos, dudv);
	
	float3 reflection = tex2D(ReflectionSampler, reflectTexCoords) * 0.5f;
		
	// color based on how much refraction vs reflection
    float3 color = lerp(waterColor, reflection, reflectionFactor);

    float specular = PhongSpecular(normalWorld, -viewDir, 256);
    
    // lerp back to refraction for the shoreline
    color = lerp(refraction, color, alpha)+ (specular * alpha);
    
    return float4(color, 1);
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
