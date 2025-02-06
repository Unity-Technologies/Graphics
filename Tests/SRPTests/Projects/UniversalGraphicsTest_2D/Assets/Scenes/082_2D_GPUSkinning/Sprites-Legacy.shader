Shader "Sprites/Legacy-GPU-Skinning"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
		HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ SKINNED_SPRITE

            // Sprite GPU Skinning : The following includes define the macros explained below.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

			struct appdata_t
			{
				float3 positionOS   : POSITION;
                float4 color        : COLOR;
				float2 uv           : TEXCOORD0;
                // Sprite GPU Skinning : Add UNITY_SKINNED_VERTEX_INPUTS macro to include Blend Indices and Blend Weights Channel to Vertex Inputs.
                UNITY_SKINNED_VERTEX_INPUTS
			};

			struct v2f
			{
                float4  positionCS  : SV_POSITION;
			    float4  color       : COLOR;
                float2  uv          : TEXCOORD0;
			};

			v2f vert(appdata_t a)
			{
				v2f o;
                // Sprite GPU Skinning : Add UNITY_SKINNED_VERTEX_COMPUTE macro to compute Skinning.
                UNITY_SKINNED_VERTEX_COMPUTE(a);
                o.uv = a.uv;
			    o.color = a.color * unity_SpriteColor;
                o.positionCS = TransformObjectToHClip(a.positionOS);
				return o;
			}

			sampler2D _MainTex;
			sampler2D _AlphaTex;

			float4 SampleSpriteTexture (float2 uv)
			{
				return tex2D (_MainTex, uv);
			}

			float4 frag(v2f IN) : SV_Target
			{
				float4 c = SampleSpriteTexture (IN.uv) * IN.color;
				c.rgb *= c.a;
				return c;
			}
		ENDHLSL
		}
	}
}
