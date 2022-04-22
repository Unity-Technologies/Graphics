using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a blackboard for a graph.
    /// </summary>
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class BlackboardGraphModel : GraphElementModel, IBlackboardGraphModel
    {
        /// <inheritdoc />
        public bool Valid => GraphModel != null;

        /// <inheritdoc />
        public virtual string GetBlackboardTitle()
        {
            return GraphModel?.GetFriendlyScriptName() ?? "";
        }

        /// <inheritdoc />
        public virtual string GetBlackboardSubTitle()
        {
            return "Class Library";
        }
    }
}
