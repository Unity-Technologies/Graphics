using System;

namespace UnityEngine.Rendering.Universal
{
    public class UniversalRenderPipelineDebugDisplaySettings : DebugDisplaySettings<UniversalRenderPipelineDebugDisplaySettings>
    {
        DebugDisplaySettingsCommon CommonSettings { get; set; }

        /// <summary>
        /// Material-related Rendering Debugger settings.
        /// </summary>
        internal DebugDisplaySettingsMaterial MaterialSettings { get; private set; }

        /// <summary>
        /// Rendering-related Rendering Debugger settings.
        /// </summary>
        internal DebugDisplaySettingsRendering RenderingSettings { get; private set; }

        /// <summary>
        /// Lighting-related Rendering Debugger settings.
        /// </summary>
        internal DebugDisplaySettingsLighting LightingSettings { get; private set; }

        #region IDebugDisplaySettingsQuery

        /// <summary>
        /// Returns true if any of the debug settings are currently active.
        /// </summary>
        public override bool AreAnySettingsActive => MaterialSettings.AreAnySettingsActive ||
        LightingSettings.AreAnySettingsActive ||
        RenderingSettings.AreAnySettingsActive;

        public override bool TryGetScreenClearColor(ref Color color)
        {
            return MaterialSettings.TryGetScreenClearColor(ref color) ||
                RenderingSettings.TryGetScreenClearColor(ref color) ||
                LightingSettings.TryGetScreenClearColor(ref color);
        }

        /// <summary>
        /// Returns true if lighting is active for current state of debug settings.
        /// </summary>
        public override bool IsLightingActive => MaterialSettings.IsLightingActive &&
        RenderingSettings.IsLightingActive &&
        LightingSettings.IsLightingActive;

        /// <summary>
        /// Returns true if the current state of debug settings allows post-processing.
        /// </summary>
        public override bool IsPostProcessingAllowed
        {
            get
            {
                DebugPostProcessingMode debugPostProcessingMode = RenderingSettings.debugPostProcessingMode;

                switch (debugPostProcessingMode)
                {
                    case DebugPostProcessingMode.Disabled:
                    {
                        return false;
                    }

                    case DebugPostProcessingMode.Auto:
                    {
                        // Only enable post-processing if we aren't using certain debug-views...
                        return MaterialSettings.IsPostProcessingAllowed &&
                            RenderingSettings.IsPostProcessingAllowed &&
                            LightingSettings.IsPostProcessingAllowed;
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
        }

        public override void Reset()
        {
            m_Settings.Clear();

            CommonSettings = Add(new DebugDisplaySettingsCommon());
            MaterialSettings = Add(new DebugDisplaySettingsMaterial());
            LightingSettings = Add(new DebugDisplaySettingsLighting());
            RenderingSettings = Add(new DebugDisplaySettingsRendering());
        }
    }
}
