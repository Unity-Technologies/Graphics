using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Graphing
{
    public class SlotConfigurationException : Exception
    {
        public SlotConfigurationException(string message)
            : base(message)
        { }
    }

    public static class NodeUtils
    {
        public static void SlotConfigurationExceptionIfBadConfiguration(INode node, IEnumerable<int> expectedInputSlots, IEnumerable<int> expectedOutputSlots )
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

        public static void DepthFirstCollectNodesFromNode(List<INode> nodeList, INode node, int? slotId = null, IncludeSelf includeSelf = IncludeSelf.Include)
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

            if (includeSelf == IncludeSelf.Include)
                nodeList.Add(node);
            ListPool<int>.Release(validSlots);
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
    }
}
