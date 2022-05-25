// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Custom/VertexColor" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_BiasColor("Bias Color", Color) = (0,0,0,0)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_AlphaTex("Alpha", 2D) = "white" {}
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
	}
		SubShader{
		Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		LOD 200
		Cull off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf Standard fullforwardshadows vertex:vert alphatest:_Cutoff

		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _AlphaTex;

	struct Input {
		float2 uv_MainTex;
		float2 uv_AlphaTex;
		float4 vertColor;
	};

	half _Glossiness;
	half _Metallic;
	fixed4 _Color;
	fixed4 _BiasColor;

	void vert(inout appdata_full v, out Input o) {
		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.vertColor = v.color;
	}

	// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
	// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
	// #pragma instancing_options assumeuniformscaling
	UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutputStandard o) {
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color + _BiasColor;
		o.Albedo = c.rgb * IN.vertColor.rgb;
		o.Alpha = c.a;
	}
	ENDCG
	}
		FallBack "Transparent/Cutout/Diffuse"
}
