using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
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

            var affectedPorts = changeset.ChangedModels
                .Concat(changeset.DeletedModels)
                .Concat(changeset.NewModels)
                .OfType<GraphDataEdgeModel>()
                .Select(e => e.ToPort)
                .Where(p => p.Direction == PortDirection.Input);

            var affectedNodes = new Queue<GraphDataNodeModel>();
            foreach (var affectedPort in affectedPorts)
            {
                if (affectedPort.NodeModel is GraphDataNodeModel node)
                {
                    affectedNodes.Enqueue(node);
                }
            }

            while (affectedNodes.TryDequeue(out var node))
            {
                node.DefineNode();
                graphUpdater.MarkChanged(node);

                foreach (var outgoingEdge in node.GetOutgoingEdges())
                {
                    if (outgoingEdge is {ToPort: GraphDataPortModel {NodeModel: GraphDataNodeModel downstreamNode}})
                    {
                        affectedNodes.Enqueue(downstreamNode);
                    }
                }
            }
        }
    }
}
