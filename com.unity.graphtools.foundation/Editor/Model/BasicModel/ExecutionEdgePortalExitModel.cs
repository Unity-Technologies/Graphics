using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Model for execution exit portals.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class ExecutionEdgePortalExitModel : EdgePortalModel, IEdgePortalExitModel
    {
        /// <inheritdoc />
        public IPortModel OutputPort => NodeModelDefaultImplementations.GetOutputPort(this);

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            this.AddExecutionOutputPort("");
        }
    }
}
