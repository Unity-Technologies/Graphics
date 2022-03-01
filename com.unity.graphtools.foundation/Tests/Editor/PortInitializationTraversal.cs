using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    class PortInitializationTraversal : GraphTraversal
    {
        public List<Action<INodeModel>> Callbacks = new List<Action<INodeModel>>();
        protected override void VisitNode(INodeModel nodeModel, HashSet<INodeModel> visitedNodes)
        {
            // recurse first
            base.VisitNode(nodeModel, visitedNodes);

            if (!(nodeModel is IInputOutputPortsNodeModel node))
                return;

            foreach (var callback in Callbacks)
                callback(nodeModel);

            // do after left recursion, so the leftmost node is processed first
            foreach (var inputPortModel in node.InputsByDisplayOrder)
            {
                bool any = false;

                var connectionPortModels = inputPortModel?.GetConnectedPorts() ?? Enumerable.Empty<IPortModel>();
                foreach (var connection in connectionPortModels)
                {
                    any = true;
                    node.OnConnection(inputPortModel, connection);
                }

                if (!any)
                    node.OnConnection(inputPortModel, null);
            }

            foreach (var outputPortModel in node.OutputsByDisplayOrder)
            {
                bool any = false;

                var connectionPortModels = outputPortModel?.GetConnectedPorts() ?? Enumerable.Empty<IPortModel>();
                foreach (var connection in connectionPortModels)
                {
                    any = true;
                    node.OnConnection(outputPortModel, connection);
                }

                if (!any)
                    node.OnConnection(outputPortModel, null);
            }
        }
    }
}
