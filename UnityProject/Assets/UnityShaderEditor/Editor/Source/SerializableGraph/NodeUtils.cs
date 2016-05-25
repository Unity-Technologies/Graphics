using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    internal class NodeUtils
    {
        // GetSlotsThatOutputToNodeRecurse returns a list of output slots on from node that
        // manage to connect to toNode... they may go via some other nodes to reach there.
        // This is done by working backwards from the toNode to the fromNode as graph is one directional
        public static List<SerializableSlot> GetSlotsThatOutputToNodeRecurse(SerializableNode fromNode, SerializableNode toNode)
        {
            var foundUsedOutputSlots = new List<SerializableSlot>();
            RecurseNodesToFindValidOutputSlots(fromNode, toNode, foundUsedOutputSlots);
            return foundUsedOutputSlots;
        }

        public static void RecurseNodesToFindValidOutputSlots(SerializableNode fromNode, SerializableNode currentNode, ICollection<SerializableSlot> foundUsedOutputSlots)
        {
            if (fromNode == null || currentNode == null)
            {
                Debug.LogError("Recursing to find valid inputs on NULL node");
                return;
            }
            
            var validSlots = ListPool<SerializableSlot>.Get();
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
            ListPool<SerializableSlot>.Release(validSlots);
        }

        public static void CollectChildNodesByExecutionOrder(List<SerializableNode> nodeList, SerializableNode node, SerializableSlot slotToUse)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            var validSlots = ListPool<SerializableSlot>.Get();
            validSlots.AddRange(node.inputSlots);
            if (slotToUse != null && !validSlots.Contains(slotToUse))
            {
                ListPool<SerializableSlot>.Release(validSlots);
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

                var edges = node.owner.GetEdges(node.GetSlotReference(slot.name));
                foreach (var edge in edges)
                {
                    var outputNode = node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    CollectChildNodesByExecutionOrder(nodeList, outputNode, null);
                }
            }

            nodeList.Add(node);
            ListPool<SerializableSlot>.Release(validSlots);
        }

        public static void CollectDependentNodes(List<SerializableNode> nodeList, SerializableNode node)
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
                    CollectDependentNodes(nodeList, inputNode);
                }
            }
            nodeList.Add(node);
        }
    }
}
