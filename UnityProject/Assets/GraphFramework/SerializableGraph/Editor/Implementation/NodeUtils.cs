using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphing
{
    internal class NodeUtils
    {
        // GetSlotsThatOutputToNodeRecurse returns a list of output slots on from node that
        // manage to connect to toNode... they may go via some other nodes to reach there.
        // This is done by working backwards from the toNode to the fromNode as graph is one directional
        public static List<ISlot> GetSlotsThatOutputToNodeRecurse(INode fromNode, INode toNode)
        {
            var foundUsedOutputSlots = new List<ISlot>();
            RecurseNodesToFindValidOutputSlots(fromNode, toNode, foundUsedOutputSlots);
            return foundUsedOutputSlots;
        }

        public static void RecurseNodesToFindValidOutputSlots(INode fromNode, INode currentNode, ICollection<ISlot> foundUsedOutputSlots)
        {
            if (fromNode == null || currentNode == null)
            {
                Debug.LogError("Recursing to find valid inputs on NULL node");
                return;
            }
            
            var validSlots = ListPool<ISlot>.Get();
            validSlots.AddRange(currentNode.inputSlots);
            for (int index = 0; index < validSlots.Count; index++)
            {
                var inputSlot = validSlots[index];
                var edges = currentNode.owner.GetEdges(currentNode.GetSlotReference(inputSlot.name));
                foreach (var edge in edges)
                {
                    var outputNode = currentNode.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    var outputSlot = outputNode.FindOutputSlot(edge.outputSlot.slotName);
                    if (outputNode == fromNode && !foundUsedOutputSlots.Contains(outputSlot))
                        foundUsedOutputSlots.Add(outputSlot);
                    else
                        RecurseNodesToFindValidOutputSlots(fromNode, outputNode, foundUsedOutputSlots);
                }
            }
            ListPool<ISlot>.Release(validSlots);
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
            if (slotToUse != null && node.inputSlots.All(x => x.name != slotToUse.name))
                return;

            var validSlots = ListPool<ISlot>.Get();
            if (slotToUse != null)
                validSlots.Add(slotToUse);
            else
                validSlots.AddRange(node.inputSlots);
        
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

            foreach (var slot in node.outputSlots)
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
