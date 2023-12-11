namespace UnityEngine.Rendering
{
    /// <summary>
    /// Interface for a class that provides access to the asset used to initialize the default volume profile.
    /// </summary>
    public interface IDefaultVolumeProfileAsset : IRenderPipelineGraphicsSettings
    {
        /// <summary>
        /// The volume profile asset used to initialize Default Volume Profile.
        /// </summary>
        public VolumeProfile defaultVolumeProfile { get; set; }
    }
}
