
namespace UnityEngine.Rendering
{
    /// <summary>
    /// Interface defining if an SRP supports environment effects for lens flare occlusion
    /// </summary>
    public interface ICloudBackground
    {
        /// <summary>
        /// Check is the current Render Pipeline supports environement effects for lens flare occlusion.
        /// </summary>
        /// <returns>true if environement effects may occlude lens flares.</returns>
        public bool IsCloudBackgroundUsable();
    }
}
