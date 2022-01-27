using System;

namespace UnityEngine.Rendering.Universal
{
    public class UniversalRenderPipelineDebugDisplaySettings : DebugDisplaySettings<UniversalRenderPipelineDebugDisplaySettings>
    {
        DebugDisplaySettingsCommon CommonSettings { get; set; }

        /// <summary>
        /// Material-related Rendering Debugger settings.
        /// </summary>
        public DebugDisplaySettingsMaterial materialSettings { get; private set; }

        /// <summary>
        /// Rendering-related Rendering Debugger settings.
        /// </summary>
        public DebugDisplaySettingsRendering renderingSettings { get; private set; }

        /// <summary>
        /// Lighting-related Rendering Debugger settings.
        /// </summary>
        public DebugDisplaySettingsLighting lightingSettings { get; private set; }

        /// <summary>
        /// Volume-related Rendering Debugger settings.
        /// </summary>
        public DebugDisplaySettingsVolume volumeSettings { get; private set; }

        /// <summary>
        /// Display stats.
        /// </summary>
        internal DebugDisplayStats DisplayStats { get; private set; }

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

        public UniversalRenderPipelineDebugDisplaySettings()
        {
            Reset();
        }

        public override void Reset()
        {
            m_Settings.Clear();

            DisplayStats = Add(new DebugDisplayStats());
            CommonSettings = Add(new DebugDisplaySettingsCommon());
            materialSettings = Add(new DebugDisplaySettingsMaterial());
            lightingSettings = Add(new DebugDisplaySettingsLighting());
            renderingSettings = Add(new DebugDisplaySettingsRendering());
            volumeSettings = Add(new DebugDisplaySettingsVolume(new UniversalRenderPipelineVolumeDebugSettings()));
        }

        internal void UpdateFrameTiming()
        {
            if (DisplayStats != null)
                DisplayStats.UpdateFrameTiming();
        }
    }
}
