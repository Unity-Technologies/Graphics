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

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ENDHLSL

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
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SpriteMaskShared.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags{ "LightMode" = "NormalsRendering" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SpriteMaskShared.hlsl"
            ENDHLSL
        }

    }
}
