using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Placeholder displayed when an editor cannot be created for a field.
    /// </summary>
    public class MissingFieldEditor : BaseModelPropertyField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingFieldEditor"/> class.
        /// </summary>
        /// <param name="commandTarget">The view to use to dispatch commands when the field is edited.</param>
        /// <param name="fieldLabel">The field label.</param>
        public MissingFieldEditor(ICommandTarget commandTarget, string fieldLabel)
            : base(commandTarget)
        {
            var label = new Label($"Missing editor for: {fieldLabel}.");
            Add(label);
        }

        /// <inheritdoc />
        public override bool UpdateDisplayedValue()
        {
            return true;
        }
    }
}
