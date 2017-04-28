uint SpaceIndex;

StructuredBuffer<float3> sbPos;
StructuredBuffer<float2> sbSize;
StructuredBuffer<float4> sbColor;

int SizeCount=1;
int ColorCount=1;

Texture2D tex0 <string uiname="Texture";>;

SamplerState g_samLinear : IMMUTABLE
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

cbuffer cbPerDraw : register(b0)
{
	float4x4 tVP:VIEWPROJECTION;
	float4x4 tV:VIEW;
	float4x4 tVI:VIEWINVERSE;
	float4x4 tP:PROJECTION;
	 
	uint PositionStartIndex;
	uint SizeStartIndex;
	uint ColorStartIndex;
};

cbuffer cbPerObject : register(b1)
{
    float4x4 tW : WORLD;
    uint di : DRAWINDEX;
}

struct VS_IN
{
	uint vi : SV_VertexID;
};

struct VS_OUT
{
	float4 PosWVP:SV_POSITION;
	float2 TexCd:TEXCOORD0;
	float4 PosWV:TEXCOORD1;
	float2 Size:TEXCOORD2;
	float4 Color:COLOR0;	
};

int PositionIndex(uint vi)
{
	return vi + PositionStartIndex;
}

int SizeIndex(uint vi)
{
	return (vi % SizeCount) + SizeStartIndex;
}

int ColorIndex(uint vi)
{
	return (vi % ColorCount) + ColorStartIndex;
}

static float4x4 Identity =
{
    { 1, 0, 0, 0 },
    { 0, 1, 0, 0 },
    { 0, 0, 1, 0 },
    { 0, 0, 0, 1 }
};

VS_OUT VS(VS_IN In)
{
	VS_OUT Out=(VS_OUT)0;
	
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
	
	uint vi = In.vi;
	float3 p = sbPos[PositionIndex(vi)];
	
	float4 PosW = mul(float4(p, 1), tW);
	float4 PosWV = mul(PosW, view);
	
	Out.PosWV = PosWV;
	Out.PosWVP = mul(PosW, viewProj);
	Out.TexCd = 0;
	Out.Size = sbSize[SizeIndex(vi)];
	Out.Color = sbColor[ColorIndex(vi)];
	return Out;
}

float2 g_positions[4]:IMMUTABLE ={{-1,1},{1,1},{-1,-1},{1,-1}};
float2 g_texcoords[4]:IMMUTABLE ={{0,0},{1,0},{0,1},{1,1}};

[maxvertexcount(4)]
void gsSPRITE(point VS_OUT In[1], inout TriangleStream<VS_OUT> SpriteStream)
{
    VS_OUT Out = In[0];
	
	//setup spaces
	float4x4 proj = tP;
	if (SpaceIndex == 2)
	{
		proj = Identity;
	}
	
	for(int i=0;i<4;i++)
	{
		Out.TexCd = g_texcoords[i];
		Out.PosWVP = mul(float4(In[0].PosWV.xyz + float3(g_positions[i]*In[0].Size, 0), 1), tP);
		SpriteStream.Append(Out);
	}
}

[maxvertexcount(1)]
void gsPOINT(point VS_OUT In[1], inout PointStream<VS_OUT>GSOut)
{
	VS_OUT Out;	
	Out = In[0];
	Out.TexCd = float2(0.5, 0.5);
	GSOut.Append(Out);
}

float4 PS(VS_OUT In):SV_Target{
	float4 col = tex0.SampleLevel(g_samLinear,In.TexCd.xy,0);
	col *= In.Color;
	return col;
}

technique10 Sprite{
	pass P0{
		SetVertexShader(CompileShader(vs_4_0,VS()));
		SetGeometryShader(CompileShader(gs_4_0,gsSPRITE()));
		SetPixelShader(CompileShader(ps_4_0,PS()));
	}
}

technique10 Point{
	pass P0{
		SetVertexShader(CompileShader(vs_4_0,VS()));
		SetGeometryShader(CompileShader(gs_4_0,gsPOINT()));
		SetPixelShader(CompileShader(ps_4_0,PS()));
	}
}



