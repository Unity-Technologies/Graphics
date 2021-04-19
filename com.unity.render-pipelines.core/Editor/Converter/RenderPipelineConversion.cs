namespace UnityEditor.Rendering
{
    /// <summary>
    /// A class for different conversions.
    /// </summary>
    public abstract class RenderPipelineConversion
    {
        /// <summary>
        /// Name = The name of the conversion.
        /// </summary>
        public abstract string name { get; }

        /// <summary>
        /// Info = The information for this conversion.
        /// </summary>
        public abstract string info { get; }
    }
}
