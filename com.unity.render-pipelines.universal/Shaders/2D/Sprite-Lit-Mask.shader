Shader "Universal Render Pipeline/Sprite-Lit-Mask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _Cutoff ("Mask alpha cutoff", Range(0.0, 1.0)) = 0.0
        _Color ("Tint", Color) = (1,1,1,0.2)
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
        Blend Off
        ColorMask 0

        Pass
        {
            Tags{ "LightMode" = "Universal2D" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            // alpha below which a mask should discard a pixel, thereby preventing the stencil buffer from being marked with the Mask's presence
            fixed _Cutoff;

            struct appdata_masking
            {
                float4 vertex : POSITION;
                half2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f_masking
            {
                float4 pos : SV_POSITION;
                half2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f_masking vert(appdata_masking IN)
            {
                v2f_masking OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.pos = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.texcoord;

                #ifdef PIXELSNAP_ON
                OUT.pos = UnityPixelSnap (OUT.pos);
                #endif

                return OUT;
            }


            fixed4 frag(v2f_masking IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.uv);
                // for masks: discard pixel if alpha falls below MaskingCutoff
                clip (c.a - _Cutoff);
                return _Color;
            }
            ENDCG
        }

        Pass
        {
            Tags{ "LightMode" = "NormalsRendering" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            // alpha below which a mask should discard a pixel, thereby preventing the stencil buffer from being marked with the Mask's presence
            fixed _Cutoff;

            struct appdata_masking
            {
                float4 vertex : POSITION;
                half2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f_masking
            {
                float4 pos : SV_POSITION;
                half2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f_masking vert(appdata_masking IN)
            {
                v2f_masking OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.pos = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.texcoord;

                #ifdef PIXELSNAP_ON
                OUT.pos = UnityPixelSnap (OUT.pos);
                #endif

                return OUT;
            }


            fixed4 frag(v2f_masking IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.uv);
                // for masks: discard pixel if alpha falls below MaskingCutoff
                clip (c.a - _Cutoff);
                return _Color;
            }
            ENDCG
        }

    }
}
