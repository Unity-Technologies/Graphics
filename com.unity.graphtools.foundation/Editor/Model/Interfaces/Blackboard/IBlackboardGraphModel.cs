using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for the model used to display the blackboard.
    /// </summary>
    public interface IBlackboardGraphModel : IGraphElementModel
    {
        /// <summary>
        /// Whether the model is valid.
        /// </summary>
        bool Valid { get; }

        /// <summary>
        /// Gets the title of the blackboard.
        /// </summary>
        /// <returns>The title of the blackboard.</returns>
        string GetBlackboardTitle();

        /// <summary>
        /// Gets the sub-title of the blackboard.
        /// </summary>
        /// <returns>The sub-title of the blackboard.</returns>
        string GetBlackboardSubTitle();
    }
}
