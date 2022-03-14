using System;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Model for data exit portals.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class DataEdgePortalExitModel : EdgePortalModel, IEdgePortalExitModel
    {
        /// <inheritdoc />
        public IPortModel OutputPort => NodeModelDefaultImplementations.GetOutputPort(this);

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            this.AddDataOutputPort("", TypeHandle.Unknown);
        }

        /// <inheritdoc />
        public override bool CanCreateOppositePortal()
        {
            return !GraphModel.FindReferencesInGraph<IEdgePortalEntryModel>(DeclarationModel).Any();
        }
    }
}
