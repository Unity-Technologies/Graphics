using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// A class for different conversions.
    /// Name = The name of the conversion.
    /// Info = The information for this conversion.
    /// </summary>
    public abstract class RenderPipelineConversion
    {
        public abstract string name { get; }
        public abstract string info { get; }
    }
}
