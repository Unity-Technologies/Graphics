using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// This attribute allows you to add information for a class to be supported on a render pipeline
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SupportedOnAttribute : Attribute
    {
        /// <summary>
        /// The pipeline types that define the support
        /// </summary>
        public Type[] pipelineTypes { get; }

        /// <summary>
        /// Creates a new <seealso cref="SupportedOn"/> instance.
        /// </summary>
        /// <param name="pipelineTypes">The list of pipeline types
        /// create sub-menus.</param>
        public SupportedOnAttribute(params Type[] pipelineTypes)
        {
            this.pipelineTypes = pipelineTypes;
        }
    }

    /// <summary>
    /// Attribute used to customize UI display.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DisplayInfoAttribute : Attribute
    {
        /// <summary>Display name used in UI.</summary>
        public string name;
        /// <summary>Display order used in UI.</summary>
        public int order;
    }

    /// <summary>
    /// Attribute used to customize UI display to allow properties only be visible when "Show Additional Properties" is selected
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AdditionalPropertyAttribute : Attribute
    {
    }
}
