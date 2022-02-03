using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for views.
    /// </summary>
    public interface IModelView : ICommandTarget
    {
        /// <summary>
        /// The graph tool.
        /// </summary>
        BaseGraphTool GraphTool { get; }
    }
}
