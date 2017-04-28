
cbuffer cbLightData : register(b3)
{
	//light properties
	float3 lDir <string uiname="Light Direction";> = {0, -5, 2};        //light direction in world space
	float4 lAmb  <bool color=true; String uiname="Ambient Color";>  = {0.15, 0.15, 0.15, 1};
	float4 lSpec <bool color=true; String uiname="Specular Color";> = {0.35, 0.35, 0.35, 1};
	float lPower <String uiname="Power"; float uimin=0.0;> = 25.0;     //shininess of specular highlight
}

class PhongDir : IShading
{
	//phong directional function
	float3 Shade(float3 lDiff, float3 NormV, float3 ViewDirV, float3 LightDirV)
	{
		float3 amb = lAmb.rgb;
		
	    //halfvector
	    float3 H = normalize(ViewDirV + LightDirV);
	
	    //compute blinn lighting
	    float shades = lit(dot(NormV, LightDirV), dot(NormV, H), lPower).y;
	
	    float3 diff = lDiff * shades;
	
	    //reflection vector (view space)
	    float3 R = normalize(2 * dot(NormV, LightDirV) * NormV - LightDirV);
	
	    //normalized view direction (view space)
	    float3 V = normalize(ViewDirV);
	
	    //calculate specular light
	    float3 spec = pow(max(dot(R, V),0), lPower*.5) * lSpec.rgb;
	
	    return amb + diff + spec;
	}
};