using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Remapper/Remap Input Node")]
    public class MasterRemapInputNode : AbstractSubGraphIONode
        , INodeGroupRemapper
    {
        [NonSerialized]
        internal RemapMasterNode m_RemapTarget;

        public MasterRemapInputNode()
        {
            name = "Inputs";
        }

        public override int AddSlot()
        {
            var nextSlotId = GetOutputSlots<ISlot>().Count() + 1;
            AddSlot(new MaterialSlot(-nextSlotId, "Input " + nextSlotId, "Input" + nextSlotId, SlotType.Output, SlotValueType.Vector4, Vector4.zero));

            if (onModified != null)
            {
                onModified(this, ModificationScope.Topological);
            }

            return -nextSlotId;
        }

        public override void RemoveSlot()
        {
            var lastSlotId = GetOutputSlots<ISlot>().Count();
            if (lastSlotId == 0)
                return;

            RemoveSlot(-lastSlotId);

            if (onModified != null)
            {
                onModified(this, ModificationScope.Topological);
            }
        }

        public void DepthFirstCollectNodesFromNodeSlotList(List<INode> nodeList, NodeUtils.IncludeSelf includeSelf)
        {
            NodeUtils.DepthFirstCollectNodesFromNode(nodeList, m_RemapTarget, NodeUtils.IncludeSelf.Exclude);
        }

        public bool IsValidSlotConnection(int slotId)
        {
            if (m_RemapTarget == null)
                return false;

            var slot = m_RemapTarget.FindSlot<MaterialSlot>(slotId);
            if (slot == null)
                return false;

            var edge = m_RemapTarget.owner.GetEdges(slot.slotReference).FirstOrDefault();
            if (edge == null)
                return false;

            var outputRef = edge.outputSlot;
            var fromNode = m_RemapTarget.owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
            if (fromNode == null)
                return false;

            return true;
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            if (m_RemapTarget == null)
            {
                var defaultValueSlot = FindSlot<MaterialSlot>(slotId);
                if (defaultValueSlot == null)
                    throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");

                return defaultValueSlot.GetDefaultValue(GenerationMode.ForReals);
            }

            var slot = m_RemapTarget.FindSlot<MaterialSlot>(slotId);
            if (slot == null)
                throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");

            if (slot.isOutputSlot)
                throw new Exception(string.Format("Attempting to use OutputSlot({0}) on remap node)", slotId));

            var edge = m_RemapTarget.owner.GetEdges(slot.slotReference).FirstOrDefault();
            if (edge == null)
                return slot.GetDefaultValue(GenerationMode.ForReals);

            var outputRef = edge.outputSlot;
            var fromNode = m_RemapTarget.owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
            if (fromNode == null)
                return slot.GetDefaultValue(GenerationMode.ForReals);

            return fromNode.GetVariableNameForSlot(outputRef.slotId);
        }
    }
}
