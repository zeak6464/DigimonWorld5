Shader "Custom/SkyboxSphare" {
Properties {
	_Tint ("Tint Color", Color) = (.5, .5, .5, .5)
	[Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
	_Rotation ("Rotation", Range(0, 360)) = 0
	[NoScaleOffset] _MainTex ("Texture", 2D) = "grey" {}
}

SubShader {
	Tags { "Queue"="Background" "RenderType"="Transparent" "PreviewType"="Skybox" }
	Cull Off ZWrite Off
	
	CGINCLUDE
	#include "UnityCG.cginc"

	half4 _Tint;
	half _Exposure;
	float _Rotation;

	float3 RotateAroundYInDegrees (float3 vertex, float degrees)
	{
		float alpha = degrees * UNITY_PI / 180.0;
		float sina, cosa;
		sincos(alpha, sina, cosa);
		float2x2 m = float2x2(cosa, -sina, sina, cosa);
		return float3(mul(m, vertex.xz), vertex.y).xzy;
	}
	
	struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};
	struct v2f {
		float4 vertex : SV_POSITION;
		float2 texcoord : TEXCOORD0;
		UNITY_VERTEX_OUTPUT_STEREO
	};
	v2f vert (appdata_t v)
	{
		v2f o;
		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
		float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
		o.vertex = UnityObjectToClipPos(rotated);
		o.texcoord = v.texcoord;
		return o;
	}
	half4 skybox_frag (v2f i, sampler2D smp, half4 smpDecode)
	{
		half4 tex = tex2D (smp, i.texcoord);
		half3 c = DecodeHDR (tex, smpDecode);
		c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
		c *= _Exposure;
		return half4(c, 1);
	}
	ENDCG
	
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 2.0
		sampler2D _MainTex;
		half4 _MainTex_HDR;
		half4 frag (v2f i) : SV_Target { return skybox_frag(i, _MainTex, _MainTex_HDR); }
		ENDCG 
	}
}
}
