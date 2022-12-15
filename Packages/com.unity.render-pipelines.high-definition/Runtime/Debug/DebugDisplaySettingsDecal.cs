using System;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Decal-related Rendering Debugger settings.
    /// </summary>
    class DebugDisplaySettingsDecal : IDebugDisplaySettingsData
    {
        internal DecalsDebugSettings m_Data = new DecalsDebugSettings();

        /// <summary>Display the decal atlas.</summary>
        public bool displayAtlas
        {
            get => m_Data.displayAtlas;
            set => m_Data.displayAtlas = value;
        }

        /// <summary>Displayed decal atlas mip level.</summary>
        public UInt32 mipLevel
        {
            get => m_Data.mipLevel;
            set => m_Data.mipLevel = value;
        }

        static class Strings
        {
            public const string containerName = "Decals Affecting Transparent Objects";
            public static readonly NameAndTooltip displayAtlas = new() { name = "Display Atlas", tooltip = "Enable the checkbox to debug and display the decal atlas for a Camera in the top left of that Camera's view." };
            public static readonly NameAndTooltip mipLevel = new() { name = "Mip Level", tooltip = "Use the slider to select the mip level for the decal atlas." };
        }

        [DisplayInfo(name = "Decals", order = 5)]
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Decals";
            public SettingsPanel(DebugDisplaySettingsDecal data)
            {
                AddWidget(new DebugUI.MessageBox
                {
                    displayName = "Warning: the debug shader variants are missing. Ensure that the \"Runtime Debug Shaders\" option is enabled in HDRP Global Settings.",
                    style = DebugUI.MessageBox.Style.Warning,
                    isHiddenCallback = () =>
                    {
#if UNITY_EDITOR
                        return true;
#else
                            if (HDRenderPipelineGlobalSettings.instance != null)
                                return !HDRenderPipelineGlobalSettings.instance.stripDebugVariants;
                            return true;
#endif
                    }
                }
                    );
                AddWidget(new DebugUI.Container()
                {
                    displayName = Strings.containerName,
                    children =
                    {
                        new DebugUI.BoolField { nameAndTooltip = Strings.displayAtlas, getter = () => data.displayAtlas, setter = value => data.displayAtlas = value},
                        new DebugUI.UIntField
                        {
                            nameAndTooltip = Strings.mipLevel,
                            getter = () => data.mipLevel,
                            setter = value => data.mipLevel = value,
                            min = () => 0u,
                            max = () =>
                            {
                                int decalAtlasMipCountMax = RenderPipelineManager.currentPipeline is HDRenderPipeline hDRenderPipeline ?
                                    hDRenderPipeline.GetDecalAtlasMipCount() : CoreUtils.GetMipCount(GlobalDecalSettings.k_DefaultAtlasSize);
                                return Convert.ToUInt32(decalAtlasMipCountMax);
                            },
                            isHiddenCallback = () => !data.displayAtlas
                        }
                    }
                });
            }
        }

        #region IDebugDisplaySettingsQuery

        /// <inheritdoc/>
        public bool AreAnySettingsActive => displayAtlas || mipLevel > 0;

        /// <inheritdoc/>
        public bool IsPostProcessingAllowed => !AreAnySettingsActive;

        /// <inheritdoc/>
        public bool IsLightingActive => !AreAnySettingsActive;

        /// <inheritdoc/>
        public bool TryGetScreenClearColor(ref Color color)
        {
            return false;
        }

        /// <inheritdoc/>
        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
