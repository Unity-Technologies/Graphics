Shader "Hidden/Universal Render Pipeline/PaniniProjection"
{
    Properties
    {
        _MainTex("Source", 2D) = "white" {}
    }

    HLSLINCLUDE

        #pragma multi_compile_local _GENERIC _UNIT_DISTANCE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        TEXTURE2D_X(_MainTex);

        float4 _Params;

        // Back-ported & adapted from the work of the Stockholm demo team - thanks Lasse
        float2 Panini_UnitDistance(float2 view_pos)
        {
            // Given
            //    S----------- E--X-------
            //    |      ` .  /,´
            //    |-- ---    Q
            //  1 |       ,´/  `
            //    |     ,´ /    ´
            //    |   ,´  /      `
            //    | ,´   /       .
            //    O`    /        .
            //    |    /         `
            //    |   /         ´
            //  1 |  /         ´
            //    | /        ´
            //    |/_  .  ´
            //    P
            //
            // Have E
            // Want to find X
            //
            // First apply tangent-secant theorem to find Q
            //   PE*QE = SE*SE
            //   QE = PE-PQ
            //   PQ = PE-(SE*SE)/PE
            //   Q = E*(PQ/PE)
            // Then project Q to find X

            const float d = 1.0;
            const float view_dist = 2.0;
            const float view_dist_sq = 4.0;

            float view_hyp = sqrt(view_pos.x * view_pos.x + view_dist_sq);

            float cyl_hyp = view_hyp - (view_pos.x * view_pos.x) / view_hyp;
            float cyl_hyp_frac = cyl_hyp / view_hyp;
            float cyl_dist = view_dist * cyl_hyp_frac;

            float2 cyl_pos = view_pos * cyl_hyp_frac;
            return cyl_pos / (cyl_dist - d);
        }

        float2 Panini_Generic(float2 view_pos, float d)
        {
            // Given
            //    S----------- E--X-------
            //    |    `  ~.  /,´
            //    |-- ---    Q
            //    |        ,/    `
            //  1 |      ,´/       `
            //    |    ,´ /         ´
            //    |  ,´  /           ´
            //    |,`   /             ,
            //    O    /
            //    |   /               ,
            //  d |  /
            //    | /                ,
            //    |/                .
            //    P 
            //    |              ´
            //    |         , ´
            //    +-    ´
            //
            // Have E
            // Want to find X
            //
            // First compute line-circle intersection to find Q
            // Then project Q to find X

            float view_dist = 1.0 + d;
            float view_hyp_sq = view_pos.x * view_pos.x + view_dist * view_dist;

            float isect_D = view_pos.x * d;
            float isect_discrim = view_hyp_sq - isect_D * isect_D;

            float cyl_dist_minus_d = (-isect_D * view_pos.x + view_dist * sqrt(isect_discrim)) / view_hyp_sq;
            float cyl_dist = cyl_dist_minus_d + d;

            float2 cyl_pos = view_pos * (cyl_dist / view_dist);
            return cyl_pos / (cyl_dist - d);
        }

        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            #if _GENERIC
            float2 proj_pos = Panini_Generic((2.0 * input.uv - 1.0) * _Params.xy * _Params.w, _Params.z);
            #else // _UNIT_DISTANCE
            float2 proj_pos = Panini_UnitDistance((2.0 * input.uv - 1.0) * _Params.xy * _Params.w);
            #endif

            float2 proj_ndc = proj_pos / _Params.xy;
            float2 coords = proj_ndc * 0.5 + 0.5;

            return SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, coords);
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Panini Projection"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
