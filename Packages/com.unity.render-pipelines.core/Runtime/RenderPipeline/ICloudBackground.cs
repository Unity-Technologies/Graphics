
namespace UnityEngine.Rendering
{
    /// <summary>
    /// Volumetric Cloud
    /// Interface for CloudBackground on each SRP
    /// </summary>
    public interface ICloudBackground
    {
        /// <summary>
        /// Check is the current Render Pipeline had CloudBackground
        /// </summary>
        /// <returns>true if the CloudBackground is usable on the current pipeline</returns>
        public bool IsCloudBackgroundUsable();
    }
}
