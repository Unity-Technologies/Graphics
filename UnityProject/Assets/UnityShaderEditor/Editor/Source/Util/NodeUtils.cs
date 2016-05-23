using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    internal class NodeUtils
    {
        // GetSlotsThatOutputToNodeRecurse returns a list of output slots on from node that
        // manage to connect to toNode... they may go via some other nodes to reach there.
        // This is done by working backwards from the toNode to the fromNode as graph is one directional
        public static List<MaterialSlot> GetSlotsThatOutputToNodeRecurse(AbstractMaterialNode fromNode, AbstractMaterialNode toNode)
        {
            var foundUsedOutputSlots = new List<MaterialSlot>();
            RecurseNodesToFindValidOutputSlots(fromNode, toNode, foundUsedOutputSlots);
            return foundUsedOutputSlots;
        }

        public static void RecurseNodesToFindValidOutputSlots(AbstractMaterialNode fromNode, AbstractMaterialNode currentNode, ICollection<MaterialSlot> foundUsedOutputSlots)
        {
            if (fromNode == null || currentNode == null)
            {
                Debug.LogError("Recursing to find valid inputs on NULL node");
                return;
            }
            
            var validSlots = ListPool<MaterialSlot>.Get();
            validSlots.AddRange(currentNode.inputSlots);
            for (int index = 0; index < validSlots.Count; index++)
            {
                var inputSlot = validSlots[index];
                var edges = currentNode.owner.GetEdges(inputSlot);
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
            ListPool<MaterialSlot>.Release(validSlots);
        }

        public static void CollectChildNodesByExecutionOrder(ICollection<AbstractMaterialNode> nodeList, AbstractMaterialNode node, MaterialSlot slotToUse)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            var validSlots = ListPool<MaterialSlot>.Get();
            validSlots.AddRange(node.inputSlots);
            if (slotToUse != null && !validSlots.Contains(slotToUse))
            {
                ListPool<MaterialSlot>.Release(validSlots);
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
                    var outputNode = node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    CollectChildNodesByExecutionOrder(nodeList, outputNode, null);
                }
            }

            nodeList.Add(node);
            ListPool<MaterialSlot>.Release(validSlots);
        }

        public static void CollectDependentNodes(ICollection<AbstractMaterialNode> nodeList, AbstractMaterialNode node)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            foreach (var slot in node.outputSlots)
            {
                foreach (var edge in node.owner.GetEdges(slot))
                {
                    var inputNode = node.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                    CollectDependentNodes(nodeList, inputNode);
                }
            }
            nodeList.Add(node);
        }
    }
}
