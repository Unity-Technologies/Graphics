using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // TODO: This should be replaced when we're able to have GraphDelta notify the UI about reconcretization.
    public class DynamicPortObserver : StateObserver
    {
        readonly GraphModelStateComponent m_GraphModelState;

        public DynamicPortObserver(GraphModelStateComponent graphModelStateComponent)
            : base(new[] {graphModelStateComponent}, new[] {graphModelStateComponent})
        {
            m_GraphModelState = graphModelStateComponent;
        }

        public override void Observe()
        {
            using var observation = this.ObserveState(m_GraphModelState);
            using var graphUpdater = m_GraphModelState.UpdateScope;

            var changeset = m_GraphModelState.GetAggregatedChangeset(observation.LastObservedVersion);
            if (changeset == null) return;

            var affectedModels = changeset.ChangedModels
                .Concat(changeset.DeletedModels)
                .Concat(changeset.NewModels);

            var affectedNodes = new Queue<GraphDataNodeModel>();
            var seen = new HashSet<SerializableGUID>();
            foreach (var model in affectedModels)
            {
                if (model is GraphDataEdgeModel
                    {
                        ToPort: GraphDataPortModel
                        {
                            Direction: PortDirection.Input,
                            NodeModel: GraphDataNodeModel nodeModel
                        }
                    })
                {
                    affectedNodes.Enqueue(nodeModel);
                }
            }

            while (affectedNodes.TryDequeue(out var node))
            {
                if (seen.Contains(node.Guid)) continue;

                seen.Add(node.Guid);
                node.DefineNode();
                graphUpdater.MarkChanged(node);

                foreach (var outgoingEdge in node.GetOutgoingEdges())
                {
                    if (outgoingEdge.ToPort is GraphDataPortModel {NodeModel: GraphDataNodeModel downstreamNode})
                    {
                        affectedNodes.Enqueue(downstreamNode);
                    }
                }
            }
        }
    }
}
