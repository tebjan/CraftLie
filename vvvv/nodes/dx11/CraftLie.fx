//@author: vux, tonfilm
//@help: standard constant or phong shader with instancing
//@tags: color, phong
//@credits: 

Texture2D texture2d; 

SamplerState g_samLinear : IMMUTABLE
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

StructuredBuffer<float4x4> sbWorld;
StructuredBuffer<float4> sbColor;

uint TransformationCount = 1;
uint ColorCount = 1;

uint MaterialIndex;
uint SpaceIndex;

cbuffer cbPerDraw : register(b0)
{
	float4x4 tV : LAYERVIEW;
	float4x4 tP : PROJECTION;
	float4x4 tVP : LAYERVIEWPROJECTION;	
};

cbuffer cbPerObject : register(b1)
{
    float4x4 tW : WORLDLAYER;
	float4 Color  <bool color=true;>  = {1, 1, 1, 1};
	uint TransformStartIndex;	
	uint ColorStartIndex;
}

interface IShading
{
	float3 Shade(float3 DiffuseColor, float3 NormV, float3 ViewDirV, float3 LightDirV);
};

#include "PhongDirectional.fxh"

struct VS_IN
{
	uint ii : SV_InstanceID;
	float4 PosO : POSITION;
	float3 NormO : NORMAL;
	float2 TexCd : TEXCOORD0;
};

struct vs2ps
{
    float4 PosWVP: SV_POSITION;	
	float4 Color: TEXCOORD0;
    float2 TexCd: TEXCOORD1;
    float3 LightDirV: TEXCOORD2;
    float3 NormV: TEXCOORD3;
    float3 ViewDirV: TEXCOORD4;
};

int TransformIndex(uint ii)
{
	return (ii % TransformationCount) + TransformStartIndex;
}

int ColorIndex(uint ii)
{
	return (ii % ColorCount) + ColorStartIndex;
}

static float4x4 Identity =
{
    { 1, 0, 0, 0 },
    { 0, 1, 0, 0 },
    { 0, 0, 1, 0 },
    { 0, 0, 0, 1 }
};

vs2ps VS(VS_IN In)
{
    //inititalize all fields of output struct with 0
    vs2ps Out = (vs2ps)0;
	
	//setup spaces
	float4x4 view = tV;
	float4x4 viewProj = tVP;
	
	if(SpaceIndex == 1)
	{
		view = Identity;
		viewProj = tP;
	}
	else if (SpaceIndex == 2)
	{
		view = Identity;
		viewProj = Identity;
	}
	
	//instance id
	uint ii = max(In.ii, 1);
	
	float4x4 world = sbWorld[TransformIndex(ii)];
	world = mul(world, tW);
	
	if(MaterialIndex > 0)
	{
		//normal in view space
		float4x4 worldView = mul(world, view);
		
		//inverse light direction in view space
	    Out.LightDirV = normalize(-mul(float4(lDir, 0.0f), view).xyz);
		
		
	    Out.NormV = normalize(mul(In.NormO, (float3x3)worldView));
		
		//view direction
		Out.ViewDirV = normalize(mul(In.PosO.xyz, (float3x3)worldView));		
	}
	
	//position (projected)
    Out.PosWVP  = mul(In.PosO, mul(world, viewProj));
	
	Out.Color = Color * sbColor[ColorIndex(ii)];
    Out.TexCd = In.TexCd;
    return Out;
}

class ConstantColor : IShading
{
	float3 Shade(float3 DiffuseColor, float3 NormV, float3 ViewDirV, float3 LightDirV)
	{
		return DiffuseColor;
	}
};

ConstantColor cConstantColor;
PhongDir cPhongDir;

float3 Shading(vs2ps In)
{
	float3 col;
	switch (MaterialIndex)
	{
		case 0:
			col = cConstantColor.Shade(In.Color.rgb, In.NormV, In.ViewDirV, In.LightDirV);
		break;
		case 1:
			col = cPhongDir.Shade(In.Color.rgb, In.NormV, In.ViewDirV, In.LightDirV);
		break;
		default:
			col = cConstantColor.Shade(In.Color.rgb, In.NormV, In.ViewDirV, In.LightDirV);
		break;
		
	};
	
	return col;
}

float4 PS(vs2ps In): SV_Target
{
    float4 col = texture2d.Sample(g_samLinear, In.TexCd) * In.Color;
	
	col.rgb *= Shading(In);
	
    return col;
}

float4 PS_NoTex(vs2ps In): SV_Target
{
    float4 col = In.Color;
	
	col.rgb *= Shading(In);
	
    return col;
}


technique10 Constant <string noTexCdFallback="ConstantNoTexture"; >
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 ConstantNoTexture
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_NoTex() ) );
	}
}