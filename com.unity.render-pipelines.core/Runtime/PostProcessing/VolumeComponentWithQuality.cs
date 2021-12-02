namespace UnityEngine.Rendering
{
    /// <summary>
    /// Volume Component that uses Quality Settings.
    /// </summary>
    public class VolumeComponentWithQuality : VolumeComponent
    {
        /// <summary>Quality level used by this component.</summary>
        [Tooltip("Specifies the quality level to be used for performance relevant parameters.")]
        public ScalableSettingLevelParameter quality = new((int)ScalableSettingLevelParameter.Level.Medium, false);

        /// <summary>
        /// Returns true if the component uses parameters from the quality settings.
        /// </summary>
        /// <returns>True if the component uses parameters from the quality settings.</returns>
        protected bool UsesQualitySettings()
        {
            return !quality.levelAndOverride.useOverride && GraphicsSettings.currentRenderPipeline != null;
        }
    }
}
