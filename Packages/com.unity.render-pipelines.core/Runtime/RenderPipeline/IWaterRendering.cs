namespace UnityEngine.Rendering
{
    /// <summary>
    /// Water Rendering
    /// Interface for WaterRendering on each SRP
    /// </summary>
    public interface IWaterRendering
    {
        /// <summary>
        /// Check is the current Render Pipeline had WaterRendering
        /// </summary>
        /// <returns>true if the WaterRendering is usable on the current pipeline</returns>
        public bool IsWaterRenderingUsable();
    }
}
