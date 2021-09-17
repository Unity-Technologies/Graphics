namespace UnityEngine.Rendering.HighDefinition
{
    public class HDDebugDisplaySettings : DebugDisplaySettings<HDDebugDisplaySettings>
    {
        /// <summary>
        /// Material-related Rendering Debugger settings.
        /// </summary>
        internal DebugDisplaySettingsVolume VolumeSettings { get; private set; }

        #region IDebugDisplaySettingsQuery

        /// <summary>
        /// Returns true if any of the debug settings are currently active.
        /// </summary>
        public override bool AreAnySettingsActive => VolumeSettings.AreAnySettingsActive;

        public override bool TryGetScreenClearColor(ref Color color)
        {
            return VolumeSettings.TryGetScreenClearColor(ref color);
        }

        /// <summary>
        /// Returns true if lighting is active for current state of debug settings.
        /// </summary>
        public override bool IsLightingActive => VolumeSettings.IsLightingActive;

        /// <summary>
        /// Returns true if the current state of debug settings allows post-processing.
        /// </summary>
        public override bool IsPostProcessingAllowed => false;
        #endregion

        public HDDebugDisplaySettings()
        {
        }

        public override void Reset()
        {
            m_Settings.Clear();

            VolumeSettings = Add(new DebugDisplaySettingsVolume());
        }
    }
}
