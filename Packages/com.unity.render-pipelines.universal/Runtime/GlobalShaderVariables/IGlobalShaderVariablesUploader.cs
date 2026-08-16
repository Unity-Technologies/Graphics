namespace UnityEngine.Rendering.Universal
{
    internal interface IUniversalGlobalShaderVariablesUploader :
    IGlobalShaderVariablesOnly3DUploader, IGlobalShaderVariablesBaseUploader, IGlobalShaderVariablesGlobalUploader
    {}

    internal interface IUniversal2DGlobalShaderVariablesUploader :
    IGlobalShaderVariablesOnly2DUploader, IGlobalShaderVariablesBaseUploader, IGlobalShaderVariablesGlobalUploader
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
        Vector2 _GlobalMipBias { get; set; }
    }

    // Uploader surface for the variables contained in GlobalShaderVariablesOnly3D (URP 3D only).
    internal interface IGlobalShaderVariablesOnly3DUploader
    {
        Vector4 _URPDummy3D { get; set; }
    }

    // Uploader surface for the variables contained in GlobalShaderVariablesOnly2D (URP 2D only).
    internal interface IGlobalShaderVariablesOnly2DUploader
    {
        Vector4 _URPDummy2D { get; set; }
    }
}