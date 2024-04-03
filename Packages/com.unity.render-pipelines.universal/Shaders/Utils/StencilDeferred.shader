Shader "Hidden/Universal Render Pipeline/StencilDeferred"
{
    Properties {
        _StencilRef ("StencilRef", Int) = 0
        _StencilReadMask ("StencilReadMask", Int) = 0
        _StencilWriteMask ("StencilWriteMask", Int) = 0

        _LitPunctualStencilRef ("LitPunctualStencilWriteMask", Int) = 0
        _LitPunctualStencilReadMask ("LitPunctualStencilReadMask", Int) = 0
        _LitPunctualStencilWriteMask ("LitPunctualStencilWriteMask", Int) = 0

        _SimpleLitPunctualStencilRef ("SimpleLitPunctualStencilWriteMask", Int) = 0
        _SimpleLitPunctualStencilReadMask ("SimpleLitPunctualStencilReadMask", Int) = 0
        _SimpleLitPunctualStencilWriteMask ("SimpleLitPunctualStencilWriteMask", Int) = 0

        _LitDirStencilRef ("LitDirStencilRef", Int) = 0
        _LitDirStencilReadMask ("LitDirStencilReadMask", Int) = 0
        _LitDirStencilWriteMask ("LitDirStencilWriteMask", Int) = 0

        _SimpleLitDirStencilRef ("SimpleLitDirStencilRef", Int) = 0
        _SimpleLitDirStencilReadMask ("SimpleLitDirStencilReadMask", Int) = 0
        _SimpleLitDirStencilWriteMask ("SimpleLitDirStencilWriteMask", Int) = 0

        _ClearStencilRef ("ClearStencilRef", Int) = 0
        _ClearStencilReadMask ("ClearStencilReadMask", Int) = 0
        _ClearStencilWriteMask ("ClearStencilWriteMask", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        // 0 - Stencil pass
        Pass
        {
            Name "Stencil Volume"

            // -------------------------------------
            // Render State Commands
            ZTest LEQual
            ZWrite Off
            ZClip false
            Cull Off
            ColorMask 0

            // -------------------------------------
            // Stencil Settings
            Stencil {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                CompFront NotEqual
                PassFront Keep
                ZFailFront Invert
                CompBack NotEqual
                PassBack Keep
                ZFailBack Invert
            }

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vertex
            #pragma fragment FragWhite

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_vertex _ _SPOT

            // -------------------------------------
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/StencilDeferred.hlsl"

            ENDHLSL
        }

        // 1 - Deferred Punctual Light (Lit)
        Pass
        {
            Name "Deferred Punctual Light (Lit)"

            // -------------------------------------
            // Render State Commands
            ZTest GEqual
            ZWrite Off
            ZClip false
            Cull Front
            Blend One One, Zero One
            BlendOp Add, Add

            // -------------------------------------
            // Stencil Settings
            Stencil {
                Ref [_LitPunctualStencilRef]
                ReadMask [_LitPunctualStencilReadMask]
                WriteMask [_LitPunctualStencilWriteMask]
                Comp Equal
                Pass Zero
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vertex
            #pragma fragment DeferredShading

            // -------------------------------------
            // Defines
            #define _LIT

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _POINT _SPOT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _DEFERRED_MIXED_LIGHTING
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS

            // -------------------------------------
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/StencilDeferred.hlsl"

            ENDHLSL
        }

        // 2 - Deferred Punctual Light (SimpleLit)
        Pass
        {
            Name "Deferred Punctual Light (SimpleLit)"

            // -------------------------------------
            // Render State Commands
            ZTest GEqual
            ZWrite Off
            ZClip false
            Cull Front
            Blend One One, Zero One
            BlendOp Add, Add

            // -------------------------------------
            // Stencil Settings
            Stencil {
                Ref [_SimpleLitPunctualStencilRef]
                ReadMask [_SimpleLitPunctualStencilReadMask]
                WriteMask [_SimpleLitPunctualStencilWriteMask]
                CompBack Equal
                PassBack Zero
                FailBack Keep
                ZFailBack Keep
            }

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vertex
            #pragma fragment DeferredShading

            // -------------------------------------
            // Defines
            #define _SIMPLELIT

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _POINT _SPOT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _DEFERRED_MIXED_LIGHTING
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS

            // -------------------------------------
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/StencilDeferred.hlsl"

            ENDHLSL
        }

        // 3 - Deferred Directional Light (Lit)
        Pass
        {
            Name "Deferred Directional Light (Lit)"

            // -------------------------------------
            // Render State Commands
            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend One SrcAlpha, Zero One
            BlendOp Add, Add

            // -------------------------------------
            // Stencil Settings
            Stencil {
                Ref [_LitDirStencilRef]
                ReadMask [_LitDirStencilReadMask]
                WriteMask [_LitDirStencilWriteMask]
                Comp Equal
                Pass Keep
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vertex
            #pragma fragment DeferredShading

            // -------------------------------------
            // Defines
            #define _LIT
            #define _DIRECTIONAL

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _DEFERRED_MAIN_LIGHT
            #pragma multi_compile_fragment _ _DEFERRED_FIRST_LIGHT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _DEFERRED_MIXED_LIGHTING
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS

            // -------------------------------------
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/StencilDeferred.hlsl"

            ENDHLSL
        }

        // 4 - Deferred Directional Light (SimpleLit)
        Pass
        {
            Name "Deferred Directional Light (SimpleLit)"

            // -------------------------------------
            // Render State Commands
            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend One SrcAlpha, Zero One
            BlendOp Add, Add

            // -------------------------------------
            // Stencil Settings
            Stencil {
                Ref [_SimpleLitDirStencilRef]
                ReadMask [_SimpleLitDirStencilReadMask]
                WriteMask [_SimpleLitDirStencilWriteMask]
                Comp Equal
                Pass Keep
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vertex
            #pragma fragment DeferredShading

            // -------------------------------------
            // Universal Pipeline keywords
            #define _SIMPLELIT
            #define _DIRECTIONAL

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _DEFERRED_MAIN_LIGHT
            #pragma multi_compile_fragment _ _DEFERRED_FIRST_LIGHT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _DEFERRED_MIXED_LIGHTING
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #pragma multi_compile_fragment _ _LIGHT_COOKIES

            // -------------------------------------
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/StencilDeferred.hlsl"

            ENDHLSL
        }

        // 5 - Legacy fog
        Pass
        {
            Name "Fog"

            // -------------------------------------
            // Render State Commands
            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend OneMinusSrcAlpha SrcAlpha, Zero One
            BlendOp Add, Add

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vertex
            #pragma fragment FragFog

            // -------------------------------------
            // Defines
            #define _FOG

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile FOG_LINEAR FOG_EXP FOG_EXP2
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

            // -------------------------------------
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/StencilDeferred.hlsl"

            ENDHLSL
        }

        // 6 - Clear stencil partial
        // This pass clears stencil between camera stacks rendering.
        // This is because deferred renderer encodes material properties in the 4 highest bits of the stencil buffer,
        // but we don't want to keep this information between camera stacks.
        Pass
        {
            Name "ClearStencilPartial"

            // -------------------------------------
            // Render State Commands
            ColorMask 0
            ZTest NotEqual
            ZWrite Off
            Cull Off

            // -------------------------------------
            // Stencil Settings
            Stencil {
                Ref [_ClearStencilRef]
                ReadMask [_ClearStencilReadMask]
                WriteMask [_ClearStencilWriteMask]
                Comp NotEqual
                Pass Zero
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vertex
            #pragma fragment FragWhite

            // -------------------------------------
            // Defines
            #define _CLEAR_STENCIL_PARTIAL

            // -------------------------------------
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/StencilDeferred.hlsl"

            ENDHLSL
        }

        // 7 - SSAO Only
        // This pass only runs when there is no fullscreen deferred light rendered (no directional light). It will adjust indirect/baked lighting with realtime occlusion
        // by rendering just before deferred shading pass.
        // This pass is also completely discarded from vertex shader when SSAO renderer feature is not enabled.
        Pass
        {
            Name "SSAOOnly"

            // -------------------------------------
            // Render State Commands
            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend One SrcAlpha, Zero One
            BlendOp Add, Add

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vertex
            #pragma fragment FragSSAOOnly

            // -------------------------------------
            // Defines
            #define _SSAO_ONLY

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_vertex _ _SCREEN_SPACE_OCCLUSION

            // -------------------------------------
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/StencilDeferred.hlsl"

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
