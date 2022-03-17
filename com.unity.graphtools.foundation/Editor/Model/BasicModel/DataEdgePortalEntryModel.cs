using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Model for data entry portals.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class DataEdgePortalEntryModel : EdgePortalModel, IEdgePortalEntryModel
    {
        /// <inheritdoc />
        public IPortModel InputPort { get; private set; }

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            InputPort = this.AddDataInputPort("", PortDataTypeHandle);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataEdgePortalEntryModel"/> class.
        /// </summary>
        public DataEdgePortalEntryModel()
        {
            // Can't copy Data Entry portals as it makes no sense.
            this.SetCapability(Overdrive.Capabilities.Copiable, false);
        }
    }
}
