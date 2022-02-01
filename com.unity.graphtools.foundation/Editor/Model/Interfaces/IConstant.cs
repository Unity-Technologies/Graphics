using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for constants.
    /// </summary>
    public interface IConstant
    {
        /// <summary>
        /// The current value.
        /// </summary>
        object ObjectValue { get; set; }

        /// <summary>
        /// The default value.
        /// </summary>
        object DefaultValue { get; }

        /// <summary>
        /// The type of the value.
        /// </summary>
        Type Type { get; }
    }
}
