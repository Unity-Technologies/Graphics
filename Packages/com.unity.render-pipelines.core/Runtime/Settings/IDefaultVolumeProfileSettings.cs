namespace UnityEngine.Rendering
{
    /// <summary>
    /// Interface for a settings class for that stores the default volume profile for Volume Framework.
    /// </summary>
    public interface IDefaultVolumeProfileSettings : IRenderPipelineGraphicsSettings
    {
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        /// <summary>
        /// The default volume profile asset.
        /// </summary>
        public VolumeProfile volumeProfile { get; set; }
    }
}
