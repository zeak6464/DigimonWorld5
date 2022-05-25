// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Premultiplied" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Particle Texture", 2D) = "white" {}
	}

		Category{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Blend One OneMinusSrcAlpha
		ColorMask RGB
		Cull Off Lighting Off ZWrite Off Fog{ Mode Off }
		BindChannels{
		Bind "Color", color
		Bind "Vertex", vertex
		Bind "TexCoord", texcoord
	}

		// ---- Fragment program cards
		SubShader{
		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest
#pragma multi_compile_particles

#include "UnityCG.cginc"

	sampler2D _MainTex;
	fixed4 _TintColor;
	fixed4 _Color;

	struct appdata_t {
		float4 vertex : POSITION;
		fixed4 color : COLOR;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f {
		float4 vertex : POSITION;
		fixed4 color : COLOR;
		float2 texcoord : TEXCOORD0;
	};

	uniform fixed4 _MainTex_ST;

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.color = v.color;
		o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
		return o;
	}

	fixed4 frag(v2f i) : COLOR
	{
	fixed4 c = tex2D(_MainTex, i.texcoord);
	fixed4 o = _Color * fixed4(c.rgb * c.a, c.a) * i.color;
	return o;
	}
		ENDCG
	}
	}

		// ---- Dual texture cards
		SubShader{
		Pass{
		SetTexture[_MainTex]{
			combine primary * primary alpha
		}
		SetTexture[_MainTex]{
			combine previous * texture
		}
	}
	}

		// ---- Single texture cards (not entirely correct)
		SubShader{
		Pass{
		SetTexture[_MainTex]{
		combine texture * primary
	}
	}
	}
	}
}