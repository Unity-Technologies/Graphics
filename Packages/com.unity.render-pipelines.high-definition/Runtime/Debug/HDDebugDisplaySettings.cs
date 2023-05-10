namespace UnityEngine.Rendering.HighDefinition
{
    internal class HDDebugDisplaySettings : DebugDisplaySettings<HDDebugDisplaySettings>
    {
        /// <summary>
        /// Volume-related Rendering Debugger settings.
        /// </summary>
        internal DebugDisplaySettingsVolume VolumeSettings { get; private set; }


#if ENABLE_VIRTUALTEXTURES
        internal DebugDisplayVirtualTexturing vtSettings { get; private set; }
#endif

        public HDDebugDisplaySettings()
        {
        }

        public override void Reset()
        {
            base.Reset();
            VolumeSettings = Add(new DebugDisplaySettingsVolume(new HDVolumeDebugSettings()));
#if ENABLE_VIRTUALTEXTURES
            vtSettings = Add(new DebugDisplayVirtualTexturing());
#endif
        }
    }
}
