using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Meant to represent a GTF Port Model that is backed by a CLDS entry at the graph data level
    /// </summary>
    public class GraphDataPortModel : PortModel
    {
        /// <summary>
        /// Used to retrieve CLDS data about this port model, currently set to be the GTF Guid of this port model on creation
        /// </summary>
        public string graphDataName => UniqueName;

        /// <summary>
        /// Represents the entity that owns this port, current classes that implement this are GraphDataNodeModel and GraphDataVariableNodeModel
        /// </summary>
        public IGraphDataOwner owner => (IGraphDataOwner)NodeModel;
    }
}
