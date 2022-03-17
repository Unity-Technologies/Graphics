using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IRootView : IBaseModelView, ICommandTarget
    {
        /// <summary>
        /// The graph tool.
        /// </summary>
        BaseGraphTool GraphTool { get; }

        /// <summary>
        /// The <see cref="EditorWindow"/> containing this view.
        /// </summary>
        EditorWindow Window { get; }
    }
}
