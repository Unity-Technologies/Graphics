using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class EdgeOrderObserver : StateObserver
    {
        SelectionStateComponent m_SelectionState;
        GraphModelStateComponent m_GraphModelState;

        public EdgeOrderObserver(SelectionStateComponent selectionState, GraphModelStateComponent graphModelState)
            : base(new[] { selectionState },
                new[] { graphModelState })
        {
            m_SelectionState = selectionState;
            m_GraphModelState = graphModelState;
        }

        public override void Observe()
        {
            using (var selObs = this.ObserveState(m_SelectionState))
            {
                List<IGraphElementModel> changedModels = null;

                if (selObs.UpdateType == UpdateType.Complete)
                {
                    changedModels = m_GraphModelState.GraphModel.EdgeModels.Concat<IGraphElementModel>(m_GraphModelState.GraphModel.NodeModels).ToList();
                }
                else if (selObs.UpdateType == UpdateType.Partial)
                {
                    var changeset = m_SelectionState.GetAggregatedChangeset(selObs.LastObservedVersion);
                    changedModels = changeset.ChangedModels.ToList();
                }

                if (changedModels != null)
                {
                    var portsToUpdate = new HashSet<IReorderableEdgesPortModel>();

                    foreach (var model in changedModels.OfType<IEdgeModel>())
                    {
                        if (model.FromPort is IReorderableEdgesPortModel reorderableEdgesPort && reorderableEdgesPort.HasReorderableEdges)
                        {
                            portsToUpdate.Add(reorderableEdgesPort);
                        }
                    }

                    foreach (var model in changedModels.OfType<IPortNodeModel>())
                    {
                        foreach (var port in model.Ports
                                 .OfType<IReorderableEdgesPortModel>()
                                 .Where(p => p.HasReorderableEdges))
                        {
                            portsToUpdate.Add(port);
                        }
                    }

                    if (portsToUpdate.Count > 0)
                    {
                        using (var updater = m_GraphModelState.UpdateScope)
                        {
                            foreach (var portModel in portsToUpdate)
                            {
                                var connectedEdges = portModel.GetConnectedEdges().ToList();

                                var selected = m_SelectionState.IsSelected(portModel.NodeModel);
                                if (!selected)
                                {
                                    foreach (var edgeModel in connectedEdges)
                                    {
                                        selected = m_SelectionState.IsSelected(edgeModel);
                                        if (selected)
                                            break;
                                    }
                                }

                                updater.MarkChanged(connectedEdges, ChangeHint.Style);
                            }
                        }
                    }
                }
            }
        }
    }
}
