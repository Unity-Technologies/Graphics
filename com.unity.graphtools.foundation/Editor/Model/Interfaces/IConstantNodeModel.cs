using System;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for constant node.
    /// </summary>
    public interface IConstantNodeModel : ISingleOutputPortNodeModel
    {
        /// <summary>
        /// Sets the value of the constant in a type-safe manner.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        void SetValue<T>(T value);

        /// <summary>
        /// The value, as an <see cref="object"/>.
        /// </summary>
        object ObjectValue { get; set; }

        /// <summary>
        /// The <see cref="Type"/> of the value.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Whether the constant is locked or not.
        /// </summary>
        bool IsLocked { get; set; }

        /// <summary>
        /// The value of the node.
        /// </summary>
        IConstant Value { get; }

        /// <summary>
        /// Initializes the node.
        /// </summary>
        /// <param name="constantTypeHandle">The type of value held by the node.</param>
        void Initialize(TypeHandle constantTypeHandle);
    }
}
