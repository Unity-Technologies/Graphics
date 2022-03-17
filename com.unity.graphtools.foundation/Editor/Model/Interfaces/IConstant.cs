using System;
using UnityEngine.GraphToolsFoundation.Overdrive;

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

        /// <summary>
        /// Initializes the constant after creation.
        /// </summary>
        void Initialize(TypeHandle constantTypeHandle);

        /// <summary>
        /// Clones the constant.
        /// </summary>
        /// <returns>The cloned constant.</returns>
        IConstant Clone();

        /// <summary>
        /// Gets the <see cref="TypeHandle"/> of the value.
        /// </summary>
        /// <returns>The <see cref="TypeHandle"/> of the value.</returns>
        TypeHandle GetTypeHandle();
    }
}
