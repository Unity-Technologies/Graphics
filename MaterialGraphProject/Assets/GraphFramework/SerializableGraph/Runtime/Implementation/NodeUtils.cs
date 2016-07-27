using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Graphing
{
    public static class NodeUtils
    {
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
        public static void DepthFirstCollectNodesFromNode(List<INode> nodeList, INode node, int? slotId = null, bool includeSelf = true)
        {
            // no where to start
            if (node == null)
                return;

            // allready added this node
            if (nodeList.Contains(node))
                return;
            
            // if we have a slot passed in but can not find it on the node abort
            if (slotId.HasValue && node.GetInputSlots<ISlot>().All(x => x.id != slotId.Value))
                return;

            var validSlots = ListPool<int>.Get();
            if (slotId.HasValue)
                validSlots.Add(slotId.Value);
            else
                validSlots.AddRange(node.GetInputSlots<ISlot>().Select(x => x.id));
        
            foreach (var slot in validSlots)
            {
                foreach (var edge in node.owner.GetEdges(node.GetSlotReference(slot)))
                {
                    var outputNode = node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    DepthFirstCollectNodesFromNode(nodeList, outputNode);
                }
            }

            if (includeSelf)
                nodeList.Add(node);
            ListPool<int>.Release(validSlots);
        }

        public static void CollectNodesNodeFeedsInto(List<INode> nodeList, INode node, bool includeSelf = true)
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
            if (includeSelf)
                nodeList.Add(node);
        }
    }
}
