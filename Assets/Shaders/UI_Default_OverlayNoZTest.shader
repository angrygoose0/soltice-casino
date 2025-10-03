// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UI/Default_OverlayNoZTest"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[Toggle] _ALPHA_ONLY ("Use Alpha Only (TMP)", Float) = 0
		[Toggle] _SDF ("Use SDF (TMP)", Float) = 1
		_SDFThreshold ("SDF Edge", Range(0,1)) = 0.5
		_SDFSoftness ("SDF Softness", Range(0.1,2)) = 1
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_OutlineThickness ("Outline Thickness", Range(0,1)) = 0
		
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Overlay" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
			};
			
			fixed4 _Color;
			float _ALPHA_ONLY;
			float _SDF;
			float _SDFThreshold;
			float _SDFSoftness;
			fixed4 _OutlineColor;
			float _OutlineThickness;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
#endif
				OUT.color = IN.color * _Color;
				return OUT;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f IN) : SV_Target
			{
				half4 tex = tex2D(_MainTex, IN.texcoord);
				fixed4 color;
				
				// SDF path for crisp TMP distance field fonts with optional outline
				if (_SDF > 0.5)
				{
					// Derive smooth threshold using screen-space derivatives
					float dist = tex.a;
					float w = fwidth(dist) * _SDFSoftness;
					float t = _SDFThreshold;
					// Face fill coverage
					float fill = smoothstep(t - w, t + w, dist);
					// Outline band coverage (ring): between t - _OutlineThickness and t
					float outlineT = saturate(t - _OutlineThickness);
					float outer = smoothstep(outlineT - w, outlineT + w, dist);
					float outlineBand = saturate(outer - fill);
					// Compose premultiplied-style color contributions for smooth blending
					float3 faceRGB = IN.color.rgb * fill;
					float3 outlineRGB = _OutlineColor.rgb * outlineBand;
					color.rgb = faceRGB + outlineRGB;
					color.a = max(fill, outlineBand) * IN.color.a;
				}
				else
				{
					// Non-SDF path
					if (_ALPHA_ONLY > 0.5)
					{
						color.rgb = IN.color.rgb;
						color.a = tex.a * IN.color.a;
					}
					else
					{
						color = tex * IN.color;
					}
				}
				clip (color.a - 0.01);
				return color;
			}
		ENDCG
		}
	}
}


