
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsRendering : IDebugDisplaySettingsData
    {
        internal FullScreenDebugMode fullScreenDebugMode { get; private set; } = FullScreenDebugMode.None;
        internal SceneOverrides sceneOverrides { get; private set; } = SceneOverrides.None;
        internal DebugMipInfo mipInfoDebugMode { get; private set; } = DebugMipInfo.None;

        public PostProcessingState postProcessingState { get; private set; } = PostProcessingState.Auto;
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

                AddWidget(new DebugUI.EnumField { displayName = "Post-processing", autoEnum = typeof(PostProcessingState), getter = () => (int)data.postProcessingState, setter = (value) => data.postProcessingState = (PostProcessingState)value, getIndex = () => (int)data.postProcessingState, setIndex = (value) => data.postProcessingState = (PostProcessingState)value});
                AddWidget(new DebugUI.BoolField { displayName = "MSAA", getter = () => data.enableMsaa, setter = (value) => data.enableMsaa = value });
                AddWidget(new DebugUI.BoolField { displayName = "HDR", getter = () => data.enableHDR, setter = (value) => data.enableHDR = value });
            }
        }

        #region IDebugDisplaySettingsData
        public bool AreAnySettingsActive => enableMsaa || enableHDR ||
                                            (postProcessingState != PostProcessingState.Disabled) ||
                                            (fullScreenDebugMode != FullScreenDebugMode.None) ||
                                            (sceneOverrides != SceneOverrides.None);

        public bool IsPostProcessingAllowed => (postProcessingState != PostProcessingState.Disabled) &&
                                               (sceneOverrides == SceneOverrides.None);

        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }
        #endregion
    }
}
