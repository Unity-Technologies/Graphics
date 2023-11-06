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

        /// <summary>
        /// GPU Resident Drawer Rendering Debugger settings and statistics.
        /// </summary>
        internal DebugDisplayGPUResidentDrawer gpuResidentDrawerSettings { get; private set; }

#if ENABLE_VIRTUALTEXTURES
        internal DebugDisplayVirtualTexturing vtSettings { get; private set; }
#endif

        public HDDebugDisplaySettings()
        {
        }

        public override void Reset()
        {
            base.Reset();
            displayStats = Add(new DebugDisplaySettingsStats<HDProfileId>(new HDDebugDisplayStats()));
            volumeSettings = Add(new DebugDisplaySettingsVolume(new HDVolumeDebugSettings()));
            decalSettings = Add(new DebugDisplaySettingsDecal());
            gpuResidentDrawerSettings = Add(new DebugDisplayGPUResidentDrawer());
#if ENABLE_VIRTUALTEXTURES
            vtSettings = Add(new DebugDisplayVirtualTexturing());
#endif
        }

        internal void UpdateDisplayStats()
        {
            if (displayStats != null)
                displayStats.debugDisplayStats.Update();
        }
    }
}
