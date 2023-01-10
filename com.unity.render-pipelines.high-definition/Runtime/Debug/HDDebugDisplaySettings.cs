namespace UnityEngine.Rendering.HighDefinition
{
    internal class HDDebugDisplaySettings : DebugDisplaySettings<HDDebugDisplaySettings>
    {
        /// <summary>
        /// Rendering Debugger display stats.
        /// </summary>
        internal DebugDisplaySettingsStats<HDProfileId> displayStats { get; private set; }

        /// <summary>
        /// Volume-related Rendering Debugger settings.
        /// </summary>
        internal DebugDisplaySettingsVolume volumeSettings { get; private set; }

        /// <summary>
        /// Decal-related Rendering Debugger settings.
        /// </summary>
        internal DebugDisplaySettingsDecal decalSettings { get; private set; }

        public HDDebugDisplaySettings()
        {
        }

        public override void Reset()
        {
            base.Reset();
            displayStats = Add(new DebugDisplaySettingsStats<HDProfileId>(new HDDebugDisplayStats()));
            volumeSettings = Add(new DebugDisplaySettingsVolume(new HDVolumeDebugSettings()));
            decalSettings = Add(new DebugDisplaySettingsDecal());
        }

        internal void UpdateDisplayStats()
        {
            if (displayStats != null)
                displayStats.debugDisplayStats.Update();
        }
    }
}
