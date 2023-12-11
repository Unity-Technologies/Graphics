using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class for the rendering debugger settings.
    /// </summary>
    public class UniversalRenderPipelineDebugDisplaySettings : DebugDisplaySettings<UniversalRenderPipelineDebugDisplaySettings>
    {
        DebugDisplaySettingsCommon commonSettings { get; set; }

        /// <summary>
        /// Material-related rendering debugger settings.
        /// </summary>
        public DebugDisplaySettingsMaterial materialSettings { get; private set; }

        /// <summary>
        /// Rendering-related rendering debugger settings.
        /// </summary>
        public DebugDisplaySettingsRendering renderingSettings { get; private set; }

        /// <summary>
        /// Lighting-related rendering debugger settings.
        /// </summary>
        public DebugDisplaySettingsLighting lightingSettings { get; private set; }

        /// <summary>
        /// Volume-related rendering debugger settings.
        /// </summary>
        public DebugDisplaySettingsVolume volumeSettings { get; private set; }

        /// <summary>
        /// Display stats.
        /// </summary>
        internal DebugDisplaySettingsStats<URPProfileId> displayStats { get; private set; }

        /// <summary>
        /// GPU Resident Drawer Rendering Debugger settings and statistics.
        /// </summary>
        internal DebugDisplayGPUResidentDrawer gpuResidentDrawerSettings { get; private set; }

        #region IDebugDisplaySettingsQuery

        /// <summary>
        /// Returns true if the current state of debug settings allows post-processing.
        /// </summary>
        public override bool IsPostProcessingAllowed
        {
            get
            {
                DebugPostProcessingMode debugPostProcessingMode = renderingSettings.postProcessingDebugMode;

                switch (debugPostProcessingMode)
                {
                    case DebugPostProcessingMode.Disabled:
                    {
                        return false;
                    }

                    case DebugPostProcessingMode.Auto:
                    {
                        // Only enable post-processing if we aren't using certain debug-views.
                        bool postProcessingAllowed = true;
                        foreach (IDebugDisplaySettingsData setting in m_Settings)
                            postProcessingAllowed &= setting.IsPostProcessingAllowed;
                        return postProcessingAllowed;
                    }

                    case DebugPostProcessingMode.Enabled:
                    {
                        return true;
                    }

                    default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(debugPostProcessingMode), $"Invalid post-processing state {debugPostProcessingMode}");
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Creates a new <c>UniversalRenderPipelineDebugDisplaySettings</c> instance.
        /// </summary>
        public UniversalRenderPipelineDebugDisplaySettings()
        {
            Reset();
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();

            displayStats = Add(new DebugDisplaySettingsStats<URPProfileId>(new UniversalRenderPipelineDebugDisplayStats()));
            materialSettings = Add(new DebugDisplaySettingsMaterial());
            lightingSettings = Add(new DebugDisplaySettingsLighting());
            renderingSettings = Add(new DebugDisplaySettingsRendering());
            volumeSettings = Add(new DebugDisplaySettingsVolume(new UniversalRenderPipelineVolumeDebugSettings()));
            commonSettings = Add(new DebugDisplaySettingsCommon());
            gpuResidentDrawerSettings = Add(new DebugDisplayGPUResidentDrawer());

            // This is not a debug property owned by any `IDebugDisplaySettingsData`, it is a static property on `Texture`.
            // When the user hits reset, we want to make sure texture mip caching is enabled again (regardless of whether the
            // user toggled this in the Rendering Debugger UI or changed it using the scripting API).
            Texture.streamingTextureDiscardUnusedMips = false;
        }

        internal void UpdateDisplayStats()
        {
            if (displayStats != null)
                displayStats.debugDisplayStats.Update();
        }

        internal void UpdateMaterials()
        {
            if (renderingSettings.mipInfoMode != DebugMipInfoMode.None)
            {
                int textureSlotImpl = (renderingSettings.canAggregateData && renderingSettings.showInfoForAllSlots)
                    ? -1
                    : renderingSettings.mipDebugMaterialTextureSlot;
                Texture.SetStreamingTextureMaterialDebugProperties(textureSlotImpl);
            }
        }
    }
}
