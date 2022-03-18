using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A model to hold error messages to be displayed by badges.
    /// </summary>
    public interface IErrorBadgeModel : IBadgeModel
    {
        LogType ErrorType { get; }

        /// <summary>
        /// The error message to display.
        /// </summary>
        string ErrorMessage { get; }
    }
}
