using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.MaterialGraph;

namespace UnityEngine.Graphing
{
    public class SlotConfigurationException : Exception
    {
        public SlotConfigurationException(string message)
            : base(message)
        {}
    }

    public static class NodeUtils
    {
        public static void SlotConfigurationExceptionIfBadConfiguration(INode node, IEnumerable<int> expectedInputSlots, IEnumerable<int> expectedOutputSlots)
        {
            var missingSlots = new List<int>();

            var inputSlots = expectedInputSlots as IList<int> ?? expectedInputSlots.ToList();
            missingSlots.AddRange(inputSlots.Except(node.GetInputSlots<ISlot>().Select(x => x.id)));

            var outputSlots = expectedOutputSlots as IList<int> ?? expectedOutputSlots.ToList();
            missingSlots.AddRange(outputSlots.Except(node.GetOutputSlots<ISlot>().Select(x => x.id)));

            if (missingSlots.Count == 0)
                return;

            var toPrint = missingSlots.Select(x => x.ToString());

            throw new SlotConfigurationException(string.Format("Missing slots {0} on node {1}", string.Join(", ", toPrint.ToArray()), node));
        }

        public static IEnumerable<IEdge> GetAllEdges(INode node)
        {
            var result = new List<IEdge>();
            var validSlots = ListPool<ISlot>.Get();

            validSlots.AddRange(node.GetInputSlots<ISlot>());
            for (int index = 0; index < validSlots.Count; index++)
            {
                var inputSlot = validSlots[index];
                result.AddRange(node.owner.GetEdges(inputSlot.slotReference));
            }

            validSlots.Clear();
            validSlots.AddRange(node.GetOutputSlots<ISlot>());
            for (int index = 0; index < validSlots.Count; index++)
            {
                var outputSlot = validSlots[index];
                result.AddRange(node.owner.GetEdges(outputSlot.slotReference));
            }

            ListPool<ISlot>.Release(validSlots);
            return result;
        }

        // CollectNodesNodeFeedsInto looks at the current node and calculates
        // which child nodes it depends on for it's calculation.
        // Results are returned depth first so by processing each node in
        // order you can generate a valid code block.
        public enum IncludeSelf
        {
            Include,
            Exclude
        }

        public static void DepthFirstCollectNodesFromNode(List<INode> nodeList, INode node, IncludeSelf includeSelf = IncludeSelf.Include, List<int> slotIds = null)
        {
            // no where to start
            if (node == null)
                return;

            using (var stackDisposable = StackPool<INode>.GetDisposable())
            using (var queueDisposable = QueuePool<INode>.GetDisposable())
            {
                var stack = stackDisposable.value;
                var queue = queueDisposable.value;
                queue.Enqueue(node);

                while (queue.Any())
                {
                    var fromNode = queue.Dequeue();

                    // already added this node
                    if (nodeList.Contains(fromNode))
                        continue;

                    foreach (var slot in fromNode.GetInputSlots<ISlot>())
                    {
                        if (slotIds != null && !slotIds.Contains(slot.id))
                            continue;

                        foreach (var edge in fromNode.owner.GetEdges(fromNode.GetSlotReference(slot.id)))
                        {
                            var outputNode = fromNode.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                            queue.Enqueue(outputNode);
                            stack.Push(outputNode);
                        }
                    }
                }

                while (stack.Any())
                    nodeList.Add(stack.Pop());

                if (includeSelf == IncludeSelf.Include)
                    nodeList.Add(node);
            }
        }

        public static void CollectNodesNodeFeedsInto(List<INode> nodeList, INode node, IncludeSelf includeSelf = IncludeSelf.Include)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            foreach (var slot in node.GetOutputSlots<ISlot>())
            {
                foreach (var edge in node.owner.GetEdges(slot.slotReference))
                {
                    var inputNode = node.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                    CollectNodesNodeFeedsInto(nodeList, inputNode);
                }
            }
            if (includeSelf == IncludeSelf.Include)
                nodeList.Add(node);
        }

        public static ShaderStage FindEffectiveShaderStage(INode initialNode, bool goingBackwards)
        {
            var shaderStage = ShaderStage.Dynamic;
            var nodeStack = new Stack<INode>();
            nodeStack.Push(initialNode);
            while (nodeStack.Any() && shaderStage == ShaderStage.Dynamic)
            {
                var node = nodeStack.Pop();
                foreach (var slot in goingBackwards ? node.GetInputSlots<MaterialSlot>() : node.GetOutputSlots<MaterialSlot>())
                {
                    if (shaderStage != ShaderStage.Dynamic)
                        break;
                    if (slot.shaderStage == ShaderStage.Dynamic)
                    {
                        foreach (var edge in node.owner.GetEdges(slot.slotReference))
                        {
                            var connectedNode = node.owner.GetNodeFromGuid(goingBackwards ? edge.outputSlot.nodeGuid : edge.inputSlot.nodeGuid);
                            var connectedSlot = goingBackwards ? connectedNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId) : connectedNode.FindInputSlot<MaterialSlot>(edge.inputSlot.slotId);
                            if (connectedSlot.shaderStage == ShaderStage.Dynamic)
                                nodeStack.Push(connectedNode);
                            else
                            {
                                shaderStage = connectedSlot.shaderStage;
                                break;
                            }
                        }
                    }
                    else
                        shaderStage = slot.shaderStage;
                }
            }
            return shaderStage;
        }
    }
}
