//@author: vux, tonfilm
//@help: standard constant shader
//@tags: color
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

cbuffer cbPerDraw : register( b0 )
{
	float4x4 tVP : LAYERVIEWPROJECTION;
	float4x4 tW : WORLD;
	
	uint TransformStartIndex;
	uint TransformationCount = 1;
	
	uint ColorStartIndex;
	uint ColorCount = 1;
};


struct VS_IN
{
	uint ii : SV_InstanceID;
	float4 PosO : POSITION;
	float2 TexCd : TEXCOORD0;

};

struct vs2ps
{
    float4 PosWVP: SV_POSITION;	
	float4 Color: TEXCOORD0;
    float2 TexCd: TEXCOORD1;
	
};



int TransformIndex(uint ii)
{
	return (ii % TransformationCount) + TransformStartIndex;
}

int ColorIndex(uint ii)
{
	return (ii % ColorCount) + ColorStartIndex;
}

vs2ps VS(VS_IN input)
{
    //inititalize all fields of output struct with 0
    vs2ps Out = (vs2ps)0;
	
	uint ii = input.ii;
	
	float4x4 w = sbWorld[TransformIndex(ii)];
	w=mul(w,tW);
    Out.PosWVP  = mul(input.PosO,mul(w,tVP));
	Out.Color = sbColor[ColorIndex(ii)];
    Out.TexCd = input.TexCd;
    return Out;
}




float4 PS_Tex(vs2ps In): SV_Target
{
    float4 col = texture2d.Sample( g_samLinear, In.TexCd) * In.Color;
    return col;
}





technique10 Constant
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Tex() ) );
	}
}



