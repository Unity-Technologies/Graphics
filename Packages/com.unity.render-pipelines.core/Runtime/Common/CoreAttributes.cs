using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Attribute used to customize UI display.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false)]
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
