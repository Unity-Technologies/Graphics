//Shader "Nature/Tree Soft Occlusion Bark"
Shader "TestRenderPipeline/TreeShader"
{
    Properties {
        _Color ("Main Color", Color) = (1,1,1,0)
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseLight ("Base Light", Range(0, 1)) = 0.35
        _AO ("Amb. Occlusion", Range(0, 10)) = 2.4

        // These are here only to provide default values
        [HideInInspector] _TreeInstanceColor ("TreeInstanceColor", Vector) = (1,1,1,1)
        [HideInInspector] _TreeInstanceScale ("TreeInstanceScale", Vector) = (1,1,1,1)
        [HideInInspector] _SquashAmount ("Squash", Float) = 1
    }

    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "RenderType" = "TreeOpaque"
            "DisableBatching"="True"
            "Queue" = "Geometry+1"
        }

        Pass {
            Lighting On

            Tags { "LightMode" = "Forward" }

            CGPROGRAM
            #pragma vertex bark
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityBuiltin2xTreeLibrary.cginc"

            sampler2D _MainTex;

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 col = input.color;
                col.rgb *= tex2D( _MainTex, input.uv.xy).rgb;
                UNITY_APPLY_FOG(input.fogCoord, col);
                UNITY_OPAQUE_ALPHA(col.a);
                return col;
            }
            ENDCG
        }
    }

    Dependency "BillboardShader" = "Hidden/Nature/Tree Soft Occlusion Bark Rendertex"
    Fallback Off
}
