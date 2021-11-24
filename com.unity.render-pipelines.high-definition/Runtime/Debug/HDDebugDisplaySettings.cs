namespace UnityEngine.Rendering.HighDefinition
{
    internal class HDDebugDisplaySettings : DebugDisplaySettings<HDDebugDisplaySettings>
    {
        /// <summary>
        /// Volume-related Rendering Debugger settings.
        /// </summary>
        internal DebugDisplaySettingsVolume VolumeSettings { get; private set; }

        public HDDebugDisplaySettings()
        {
        }

        public override void Reset()
        {
            base.Reset();
            VolumeSettings = Add(new DebugDisplaySettingsVolume(new HDVolumeDebugSettings()));
        }
    }
}
