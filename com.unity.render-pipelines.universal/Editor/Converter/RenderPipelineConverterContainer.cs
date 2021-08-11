namespace UnityEditor.Rendering.Universal.Converters
{
    /// <summary>
    /// A class to contain converters. This is for a common set of converters.
    /// For example: Converters that is for Built-in to URP would have it's own container.
    /// </summary>
    internal abstract class RenderPipelineConverterContainer
    {
        /// <summary>
        /// The name of the Container. This will show up int the UI.
        /// </summary>
        public abstract string name { get; }

        /// <summary>
        /// The information for this container.
        /// This will be shown in the UI to tell the user some information about the converters that this container are targeting.
        /// </summary>
        public abstract string info { get; }

        /// <summary>
        /// A priority of the Container. The lower the number (can be negative), the higher up it will be shown in the list of Containers.
        /// </summary>
        public virtual int priority => 0;
    }
}
