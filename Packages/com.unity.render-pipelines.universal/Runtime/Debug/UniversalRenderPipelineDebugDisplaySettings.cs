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
        }

        internal void UpdateDisplayStats()
        {
            if (displayStats != null)
                displayStats.debugDisplayStats.Update();
        }
    }
}
