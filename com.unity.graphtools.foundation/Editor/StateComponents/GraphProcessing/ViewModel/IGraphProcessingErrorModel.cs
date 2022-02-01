using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for graph processing errors displayed by badges.
    /// </summary>
    public interface IGraphProcessingErrorModel : IErrorBadgeModel
    {
        /// <summary>
        /// The <see cref="QuickFix"/> for the error.
        /// </summary>
        QuickFix Fix { get; }
    }
}
