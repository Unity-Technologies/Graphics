Shader "Custom/Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Metallic("Metallic", Range(0.0, 1.0)) = 1.0
        [NoScaleOffset]_MetallicGlossMap("Metallic", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        [NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}

        [HDR] _OcclusionStrength("Occlusion Strength", float) = 1.0
        [NoScaleOffset] _AmbientOcclusion("Occlusion Map", 2D) = "white" {}

        _BumpScale("Bump Scale", float) = 1.0
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        #pragma shader_feature _EMISSION
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _BaseMap;
        sampler2D _MetallicGlossMap;
        sampler2D _EmissionMap;
        sampler2D _AmbientOcclusion;
        sampler2D _BumpMap;

        struct Input
        {
            float2 uv_BaseMap;
        };

        half _Smoothness;
        half _Metallic;
        fixed4 _BaseColor;
        fixed4 _EmissionColor;
        float _OcclusionStrength;
        float _BumpScale;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float4 SampleAndScale(float2 uv, sampler2D tex, float4 scale)
        {
            return tex2D (tex, uv) * scale;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_BaseMap;
            // Albedo comes from a texture tinted by color
            fixed4 c = SampleAndScale(uv, _BaseMap, _BaseColor);
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = SampleAndScale(uv, _MetallicGlossMap, _Metallic).r;
            o.Normal = UnpackNormalWithScale(SampleAndScale(uv, _BumpMap, 1), _BumpScale);
            o.Emission = SampleAndScale(uv, _EmissionMap, _EmissionColor);
            o.Occlusion = SampleAndScale(uv, _AmbientOcclusion, _OcclusionStrength);
            o.Smoothness = _Smoothness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
