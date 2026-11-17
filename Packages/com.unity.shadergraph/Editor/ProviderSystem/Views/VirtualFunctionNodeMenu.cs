using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    static class VirtualFunctionNodeMenu
    {
        const float kOffsetFromStack = 250;
        const float kNodeSpacing = 150;

        internal static void AddActions(MaterialGraphView graphView, ContextualMenuPopulateEvent evt, IShaderNodeView nodeView)
        {
            if (!graphView.graph.isShaderInterfaceMode || nodeView.node is not AbstractVirtualFunctionNode { isOrphaned: false } node)
                return;

            evt.menu.AppendSeparator();
            if (node is not VirtualFunctionOutputNode output)
                evt.menu.AppendAction("Go to Output Node", _ => GoToOutputNode(graphView, node));
            else if (nodeView is Node view)
                AddCreateActions(graphView, evt, output, view);
        }

        internal static void GoToOutputNode(MaterialGraphView graphView, AbstractVirtualFunctionNode node)
        {
            if (!graphView.graph.VirtualFunctionOutputNodesByKey().TryGetValue(node.providerKey, out var output) || graphView.GetNodeByGuid(output.objectId) is not Node view)
                return;
            graphView.ClearSelection();
            graphView.AddToSelection(view);
            graphView.FrameSelection();
        }

        static void AddCreateActions(MaterialGraphView graphView, ContextualMenuPopulateEvent evt, VirtualFunctionOutputNode output, Node view)
        {
            var stack = view.GetFirstAncestorOfType<VirtualFunctionStackView>();
            var outputPosition = view.ChangeCoordinatesTo(graphView.contentViewContainer, Vector2.zero);
            var position = new Vector2((stack?.GetPosition().xMin ?? outputPosition.x) - kOffsetFromStack, outputPosition.y);

            var graph = graphView.graph;
            var definition = output.Provider.Definition;
            var nodes = AbstractVirtualFunctionNode.NodesFor(definition);
            for (int i = 0; i < nodes.Count; ++i)
            {
                var node = nodes[i];
                var nodePosition = position + new Vector2(0, i * kNodeSpacing);
                evt.menu.AppendAction($"Create {node.shortName} Node", _ =>
                {
                    graph.owner?.RegisterCompleteObjectUndo("Add " + node.TitleFor(definition));
                    graph.AddLocalProviderNode(node, output.Provider, nodePosition);
                });
            }
        }
    }
}
