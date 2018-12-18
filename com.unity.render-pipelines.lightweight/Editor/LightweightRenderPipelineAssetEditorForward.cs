namespace UnityEngine.Rendering.LWRP
{
    public partial class LightweightRenderPipelineAssetEditor
    {
        void DrawForwardRendererSettings()
        {
            DrawGeneralSettings();
            DrawQualitySettings();
            DrawLightingSettings();
            DrawShadowSettings();
            DrawAdvancedSettings();
        }
    }
}
