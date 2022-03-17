using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface used by constant editor builder extension methods to receive the data needed to build a constant editor.
    /// </summary>
    public interface IConstantEditorBuilder
    {
        /// <summary>
        /// The callback for when the editor value changes.
        /// </summary>
        [Obsolete]
        Action<IChangeEvent> OnValueChanged { get; }

        /// <summary>
        /// The command dispatcher.
        /// </summary>
        IRootView CommandTarget { get; }

        /// <summary>
        /// Whether the constant is locked.
        /// </summary>
        bool ConstantIsLocked { get; }

        /// <summary>
        /// The graph element model that owns the constant, if any.
        /// </summary>
        IGraphElementModel ConstantOwner { get; }

        /// <summary>
        /// The label to display in front of the field.
        /// </summary>
        string Label { get; }
    }
}
