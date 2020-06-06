using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Universal
{
    internal enum FullScreenDebugMode
    {
        None,
        Depth,
        MainLightShadowsOnly,
        AdditionalLightsShadowMap,
        MainLightShadowMap,
    }

    internal enum SceneOverrides
    {
        None,
        Overdraw,
        Wireframe,
        SolidWireframe,
    }

    public class DebugDisplaySettingsRendering : IDebugDisplaySettingsData
    {
        internal FullScreenDebugMode fullScreenDebugMode { get; private set; } = FullScreenDebugMode.None;
        internal SceneOverrides sceneOverrides { get; private set; } = SceneOverrides.None;
        internal DebugMipInfo mipInfoDebugMode { get; private set; } = DebugMipInfo.None;
        internal bool enablePostProcessing { get; private set; } = true;

        public bool enableMsaa { get; private set; } = true;
        public bool enableHDR { get; private set; } = true;


        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Rendering";

            public SettingsPanel(DebugDisplaySettingsRendering data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "Full Screen Modes", autoEnum = typeof(FullScreenDebugMode), getter = () => (int)data.fullScreenDebugMode, setter = (value) => {}, getIndex = () => (int)data.fullScreenDebugMode, setIndex = (value) => data.fullScreenDebugMode = (FullScreenDebugMode)value});
                AddWidget(new DebugUI.EnumField { displayName = "Scene Debug Modes", autoEnum = typeof(SceneOverrides), getter = () => (int)data.sceneOverrides, setter = (value) => {}, getIndex = () => (int)data.sceneOverrides, setIndex = (value) => data.sceneOverrides = (SceneOverrides)value});
                AddWidget(new DebugUI.EnumField { displayName = "Mip Modes Debug", autoEnum = typeof(DebugMipInfo), getter = () => (int)data.mipInfoDebugMode, setter = (value) => { }, getIndex = () => (int)data.mipInfoDebugMode, setIndex = (value) => data.mipInfoDebugMode = (DebugMipInfo)value });
                AddWidget(new DebugUI.BoolField { displayName = "Post-processing", getter = () => data.enablePostProcessing, setter = (value) => data.enablePostProcessing = value });
                AddWidget(new DebugUI.BoolField { displayName = "MSAA", getter = () => data.enableMsaa, setter = (value) => data.enableMsaa = value });
                AddWidget(new DebugUI.BoolField { displayName = "HDR", getter = () => data.enableHDR, setter = (value) => data.enableHDR = value });
            }
        }

        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }

        public bool IsEnabled()
        {
            return enableMsaa || enableHDR || enablePostProcessing ||
                   fullScreenDebugMode != FullScreenDebugMode.None ||
                    sceneOverrides != SceneOverrides.None;
        }
    }
}
