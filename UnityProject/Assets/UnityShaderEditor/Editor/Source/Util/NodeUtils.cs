using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.Graphs.Material
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

        public static void RecurseNodesToFindValidOutputSlots(Node fromNode, Node currentNode, ICollection<Slot> foundUsedOutputSlots)
        {
            if (fromNode == null || currentNode == null)
            {
                Debug.LogError("Recursing to find valid inputs on NULL node");
                return;
            }

            var bmn = currentNode as BaseMaterialNode;
            if (bmn == null)
                return;

            foreach (var inputSlot in bmn.GetValidInputSlots())
            {
                foreach (var edge in inputSlot.edges)
                {
                    if (edge.fromSlot.node == fromNode && !foundUsedOutputSlots.Contains(edge.fromSlot))
                        foundUsedOutputSlots.Add(edge.fromSlot);
                    else
                        RecurseNodesToFindValidOutputSlots(fromNode, edge.fromSlot.node, foundUsedOutputSlots);
                }
            }
        }

        public static void CollectChildNodesByExecutionOrder(ICollection<BaseMaterialNode> nodeList, BaseMaterialNode node, Slot slotToUse)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            var validSlots = node.GetValidInputSlots().ToList();
            if (slotToUse != null && !validSlots.Contains(slotToUse))
                return;

            var slotsToUse = slotToUse == null ? validSlots : new List<Slot> {slotToUse};

            foreach (var slot in slotsToUse)
            {
                foreach (var edge in slot.edges)
                {
                    var inputNode = edge.fromSlot.node as BaseMaterialNode;
                    CollectChildNodesByExecutionOrder(nodeList, inputNode, null);
                }
            }

            nodeList.Add(node);
        }

        public static void CollectDependentNodes(ICollection<BaseMaterialNode> nodeList, BaseMaterialNode node)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            foreach (var slot in node.outputSlots)
                foreach (var edge in slot.edges)
                    CollectDependentNodes(nodeList, edge.toSlot.node as BaseMaterialNode);

            nodeList.Add(node);
        }
    }
}
