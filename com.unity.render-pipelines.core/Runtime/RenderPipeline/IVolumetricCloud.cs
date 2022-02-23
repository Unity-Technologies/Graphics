
namespace UnityEngine.Rendering
{
    /// <summary>
    /// Volumetric Cloud
    /// Interface for VolumetricCloud on each SRP
    /// </summary>
    public interface IVolumetricCloud
    {
        /// <summary>
        /// Check is the current Render Pipeline had VolumetricCloud
        /// </summary>
        /// <returns>true if the VolumetricCloud is usable on the current pipeline</returns>
        public bool IsVolumetricCloudUsable();
    }
}
