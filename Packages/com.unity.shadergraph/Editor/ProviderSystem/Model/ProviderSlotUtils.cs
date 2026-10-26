using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    static class ProviderSlotUtils
    {
        internal const int kReturnSlotId = 0;

        internal static void Rebuild(AbstractMaterialNode node, IFieldCollection definition)
        {
            var previousIds = PreviousSlotIds(node);
            HashSet<int> usedIds = new();
            List<MaterialSlot> slots = new();

            // Return type is a special case.
            if (definition is IShaderFunction function && !ShaderObjectUtils.IsVoid(function.ReturnType))
            {
                usedIds.Add(kReturnSlotId);
                slots.Add(HeaderUtils.MakeSlotFromField(ShaderObjectUtils.MakeReturnField(function, HeaderUtils.kReturnSlotName), kReturnSlotId, SlotType.Output));
            }

            foreach (var field in definition.Fields)
            {
                if (!previousIds.TryGetValue(field.Name, out var ids))
                    continue;
                if (field.IsInput && ids.input > -1)
                    usedIds.Add(ids.input);
                if (field.IsOutput && ids.output > -1)
                    usedIds.Add(ids.output);
            }

            int nextId = kReturnSlotId;
            int NewId()
            {
                while (usedIds.Contains(++nextId));
                usedIds.Add(nextId);
                return nextId;
            }

            foreach (var field in definition.Fields)
            {
                if (!previousIds.TryGetValue(field.Name, out var ids))
                    ids = (-1, -1);

                if (field.IsInput)
                    slots.Add(HeaderUtils.MakeSlotFromField(field, ids.input > -1 ? ids.input : NewId(), SlotType.Input));
                if (field.IsOutput)
                    slots.Add(HeaderUtils.MakeSlotFromField(field, ids.output > -1 ? ids.output : NewId(), SlotType.Output));
            }

            Apply(node, slots);
        }

        internal static void Apply(AbstractMaterialNode node, IReadOnlyList<MaterialSlot> slots)
        {
            List<int> order = new();
            foreach (var slot in slots)
            {
                if (slot == null)
                    continue;

                // Space slots are rebuilt rather than updated.
                node.AddSlot(slot, slot is not SpaceMaterialSlot, slot is not SpaceMaterialSlot);
                order.Add(slot.id);
            }

            node.RemoveSlotsNameNotMatching(order, true);
            node.SetSlotOrder(order);
        }

        static Dictionary<string, (int input, int output)> PreviousSlotIds(AbstractMaterialNode node)
        {
            List<MaterialSlot> slots = new();
            node.GetSlots(slots);

            // build a map of parameter names to old slot ids (tuple is to support inout params).
            Dictionary<string, (int input, int output)> ids = new();
            foreach (var slot in slots)
            {
                if (!ids.TryGetValue(slot.shaderOutputName, out var pair))
                    pair = (-1, -1);
                if (slot.isInputSlot)
                    pair.input = slot.id;
                if (slot.isOutputSlot)
                    pair.output = slot.id;
                ids[slot.shaderOutputName] = pair;
            }
            return ids;
        }
    }
}
