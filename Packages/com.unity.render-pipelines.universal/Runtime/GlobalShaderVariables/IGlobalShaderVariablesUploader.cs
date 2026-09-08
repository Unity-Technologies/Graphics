namespace UnityEngine.Rendering.Universal
{
    internal interface IUniversalGlobalShaderVariablesUploader :
    IGlobalShaderVariablesOnly3DUploader, IGlobalShaderVariablesCommonUploader
    {}

    internal interface IUniversal2DGlobalShaderVariablesUploader :
    IGlobalShaderVariablesOnly2DUploader, IGlobalShaderVariablesCommonUploader
    {}

    // Passes used by both 2D/3D renderers can use this interface. 
    internal interface IGlobalShaderVariablesCommonUploader :
    IGlobalShaderVariablesBaseUploader, IGlobalShaderVariablesGlobalUploader
    {}

    internal interface IGlobalShaderVariablesGlobalUploader
    {
        void PushGlobal(IBaseCommandBuffer cmd);

        void ResetToDefault();

        void PushDefaultToGlobal(IBaseCommandBuffer cmd);
    }

    // Shared uploader surface for the variables contained in GlobalShaderVariablesBase.
    internal interface IGlobalShaderVariablesBaseUploader
    {
        Vector4 _Time { get; set; }
        Vector4 _SinTime { get; set; }
        Vector4 _CosTime { get; set; }
        Vector4 unity_DeltaTime { get; set; }
        Vector4 _TimeParameters { get; set; }
        Vector4 _LastTimeParameters { get; set; }
        Vector4 _ScreenParams { get; set; }
        Vector4 _ZBufferParams { get; set; }
        Vector4 unity_OrthoParams { get; set; }
        Vector4 _RTHandleScale { get; set; }
        Vector4 _ScaledScreenParams { get; set; }
        Vector4 _ScreenSize { get; set; }
        Vector4 _ScreenSizeOverride { get; set; }
        Vector4 _ScreenCoordScaleBias { get; set; }
        Vector4 unity_AmbientSky { get; set; }
        Vector4 unity_AmbientEquator { get; set; }
        Vector4 unity_AmbientGround { get; set; }
        Vector2 _GlobalMipBias { get; set; }
        float _DitheringTextureInvSize { get; set; }

        // Environment
        Vector4 _GlossyEnvironmentColor { get; set; }
        Vector4 _SubtractiveShadowColor { get; set; }
        Vector4 _GlossyEnvironmentCubeMap_HDR { get; set; }

        /// <summary>
        /// Fills the environment variables from the current render settings.
        /// </summary>
        void SetEnvironmentVars();
    }

    // Uploader surface for the variables contained in GlobalShaderVariablesOnly3D (URP 3D only).
    internal interface IGlobalShaderVariablesOnly3DUploader
    {
        // Main Light
        Matrix4x4 _MainLightWorldToLight { get; set; }
        Vector4 _MainLightPosition { get; set; }
        Vector4 _MainLightColor { get; set; }
        Vector4 _MainLightOcclusionProbes { get; set; }
        uint _MainLightLayerMask { get; set; }

        // Forward+
        Vector4 _FPParams0 { get; set; }
        Vector4 _FPParams1 { get; set; }
        Vector4 _FPParams2 { get; set; }

        // Motion Vectors
        Matrix4x4 _NonJitteredViewProjMatrix { get; set; }
        Matrix4x4 _PrevViewProjMatrix { get; set; }

        // Additional Lights
        Vector4 _AdditionalLightsCount { get; set; }

        // Main Light Shadows
        void SetMainLightWorldToShadow(in MainLightShadowMatrices matrices);
        Vector4 _CascadeShadowSplitSpheres0 { get; set; }
        Vector4 _CascadeShadowSplitSpheres1 { get; set; }
        Vector4 _CascadeShadowSplitSpheres2 { get; set; }
        Vector4 _CascadeShadowSplitSpheres3 { get; set; }
        Vector4 _CascadeShadowSplitSphereRadii { get; set; }
        Vector4 _MainLightShadowOffset0 { get; set; }
        Vector4 _MainLightShadowOffset1 { get; set; }
        Vector4 _MainLightShadowmapSize { get; set; }

        // Additional Lights Shadows
        Vector4 _AdditionalShadowOffset0 { get; set; }
        Vector4 _AdditionalShadowOffset1 { get; set; }
        Vector4 _AdditionalShadowmapSize { get; set; }

        // Misc
        float _MainLightCookieTextureFormat { get; set; }
        float _AdditionalLightsCookieAtlasTextureFormat { get; set; }
        uint _RenderingLayerMaxInt { get; set; }
        uint _EnableProbeVolumes { get; set; }
    }

    // Uploader surface for the variables contained in GlobalShaderVariablesOnly2D (URP 2D only).
    // Empty while that struct is disabled, kept so the 2D renderer keeps its own uploader surface.
    internal interface IGlobalShaderVariablesOnly2DUploader
    {
        // URP 2D-only globals, empty so disabled.
        // Vector4 _URPDummy2D { get; set; }
    }
}