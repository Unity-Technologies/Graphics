namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// A class to contain converters. This is for a common set of converters.
    /// For example: Converters that is for Built-in to URP would have it's own container.
    /// </summary>
    internal abstract class RenderPipelineConverterContainer
    {
        /// <summary>
        /// The name of the container. This name shows up in the UI.
        /// </summary>
        public abstract string name { get; }

        /// <summary>
        /// The description of the container.
        /// It is shown in the UI. Describe the converters in this container.
        /// </summary>
        public abstract string info { get; }

        /// <summary>
        /// The priority of the container. The lower the number (can be negative), the earlier Unity executes the container, and the earlier it shows up in the converter container menu.
        /// </summary>
        public virtual int priority => 0;
    }
}
