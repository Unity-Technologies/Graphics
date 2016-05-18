using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    internal class NodeUtils
    {
        // GetSlotsThatOutputToNodeRecurse returns a list of output slots on from node that
        // manage to connect to toNode... they may go via some other nodes to reach there.
        // This is done by working backwards from the toNode to the fromNode as graph is one directional
        public static List<Slot> GetSlotsThatOutputToNodeRecurse(BaseMaterialNode fromNode, BaseMaterialNode toNode)
        {
            var foundUsedOutputSlots = new List<Slot>();
            RecurseNodesToFindValidOutputSlots(fromNode, toNode, foundUsedOutputSlots);
            return foundUsedOutputSlots;
        }

        public static void RecurseNodesToFindValidOutputSlots(BaseMaterialNode fromNode, BaseMaterialNode currentNode, ICollection<Slot> foundUsedOutputSlots)
        {
            if (fromNode == null || currentNode == null)
            {
                Debug.LogError("Recursing to find valid inputs on NULL node");
                return;
            }
            
            var validSlots = ListPool<Slot>.Get();
            validSlots.AddRange(currentNode.inputSlots);
            for (int index = 0; index < validSlots.Count; index++)
            {
                var inputSlot = validSlots[index];
                var edges = currentNode.owner.GetEdges(inputSlot);
                foreach (var edge in edges)
                {
                    var outputNode = currentNode.owner.GetNodeFromGUID(edge.outputSlot.nodeGuid);
                    var outputSlot = outputNode.FindOutputSlot(edge.outputSlot.slotName);
                    if (outputNode == fromNode && !foundUsedOutputSlots.Contains(outputSlot))
                        foundUsedOutputSlots.Add(outputSlot);
                    else
                        RecurseNodesToFindValidOutputSlots(fromNode, outputNode, foundUsedOutputSlots);
                }
            }
            ListPool<Slot>.Release(validSlots);
        }

        public static void CollectChildNodesByExecutionOrder(ICollection<BaseMaterialNode> nodeList, BaseMaterialNode node, Slot slotToUse)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            var validSlots = ListPool<Slot>.Get();
            validSlots.AddRange(node.inputSlots);
            if (slotToUse != null && !validSlots.Contains(slotToUse))
            {
                ListPool<Slot>.Release(validSlots);
                return;
            }

            if (slotToUse != null)
            {
                validSlots.Clear();
                validSlots.Add(slotToUse);
            }
            
            for (int index = 0; index < validSlots.Count; index++)
            {
                var slot = validSlots[index];

                var edges = node.owner.GetEdges(slot);
                foreach (var edge in edges)
                {
                    var outputNode = node.owner.GetNodeFromGUID(edge.outputSlot.nodeGuid);
                    CollectChildNodesByExecutionOrder(nodeList, outputNode, null);
                }
            }

            nodeList.Add(node);
            ListPool<Slot>.Release(validSlots);
        }

        public static void CollectDependentNodes(ICollection<BaseMaterialNode> nodeList, BaseMaterialNode node)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            foreach (var slot in node.outputSlots)
            {
                foreach (var edge in node.owner.GetEdges(slot))
                {
                    var inputNode = node.owner.GetNodeFromGUID(edge.inputSlot.nodeGuid);
                    CollectDependentNodes(nodeList, inputNode);
                }
            }
            nodeList.Add(node);
        }
    }
}
