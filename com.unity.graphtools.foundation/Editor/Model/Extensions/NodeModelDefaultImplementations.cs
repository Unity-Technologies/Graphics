using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Default implementations for some <see cref="INodeModel"/> methods.
    /// </summary>
    /// <remarks>
    /// These methods should only be called by <see cref="INodeModel"/> related methods.
    /// </remarks>
    public static class NodeModelDefaultImplementations
    {
        /// <inheritdoc  cref="INodeModel.GetConnectedEdges"/>
        /// <param name="self">The node for which we want to get the connected edges.</param>
        public static IEnumerable<IEdgeModel> GetConnectedEdges(IPortNodeModel self)
        {
            var graphModel = self.GraphModel;
            if (graphModel != null)
                return self.Ports.SelectMany(p => graphModel.GetEdgesForPort(p));

            return Enumerable.Empty<IEdgeModel>();
        }

        /// <inheritdoc  cref="ISingleInputPortNodeModel.InputPort"/>
        /// <param name="self">The node for which we want to get the input port.</param>
        public static IPortModel GetInputPort(ISingleInputPortNodeModel self)
        {
            return self.InputsById.Values.FirstOrDefault();
        }

        /// <inheritdoc  cref="ISingleOutputPortNodeModel.OutputPort"/>
        /// <param name="self">The node for which we want to get the output port.</param>
        public static IPortModel GetOutputPort(ISingleOutputPortNodeModel self)
        {
            return self.OutputsById.Values.FirstOrDefault();
        }
    }
}
