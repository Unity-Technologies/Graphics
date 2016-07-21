using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Graphing
{
    public class NodeUtils
    {
        public static IEnumerable<IEdge> GetAllEdges(INode node)
        {
            var result = new List<IEdge>();
            var validSlots = ListPool<ISlot>.Get();

            validSlots.AddRange(node.GetInputSlots<ISlot>());
            for (int index = 0; index < validSlots.Count; index++)
            {
                var inputSlot = validSlots[index];
                result.AddRange(node.owner.GetEdges(node.GetSlotReference(inputSlot.name)));
            }

            validSlots.Clear();
            validSlots.AddRange(node.GetOutputSlots<ISlot>());
            for (int index = 0; index < validSlots.Count; index++)
            {
                var outputSlot = validSlots[index];
                result.AddRange(node.owner.GetEdges(node.GetSlotReference(outputSlot.name)));
            }

            ListPool<ISlot>.Release(validSlots);
            return result;
        }

        // CollectNodesNodeFeedsInto looks at the current node and calculates
        // which child nodes it depends on for it's calculation.
        // Results are returned depth first so by processing each node in
        // order you can generate a valid code block.
        public static void DepthFirstCollectNodesFromNode(List<INode> nodeList, INode node, ISlot slotToUse = null, bool includeSelf = true)
        {
            // no where to start
            if (node == null)
                return;

            // allready added this node
            if (nodeList.Contains(node))
                return;
            
            // if we have a slot passed in but can not find it on the node abort
            if (slotToUse != null && node.GetInputSlots<ISlot>().All(x => x.name != slotToUse.name))
                return;

            var validSlots = ListPool<ISlot>.Get();
            if (slotToUse != null)
                validSlots.Add(slotToUse);
            else
                validSlots.AddRange(node.GetInputSlots<ISlot>());
        
            for (int index = 0; index < validSlots.Count; index++)
            {
                var slot = validSlots[index];
                
                foreach (var edge in node.owner.GetEdges(node.GetSlotReference(slot.name)))
                {
                    var outputNode = node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    DepthFirstCollectNodesFromNode(nodeList, outputNode);
                }
            }

            if (includeSelf)
                nodeList.Add(node);
            ListPool<ISlot>.Release(validSlots);
        }

        public static void CollectNodesNodeFeedsInto(List<INode> nodeList, INode node, bool includeSelf = true)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            foreach (var slot in node.GetOutputSlots<ISlot>())
            {
                foreach (var edge in node.owner.GetEdges(node.GetSlotReference(slot.name)))
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
