using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // An unconnected input that has an incoming value (a Base argument, an Output inout) passes it through.
    internal enum SlotLayout
    {
        AsDeclared,
        AsDeclaredPassthrough,

        // Out and inout parameters and the return value, as inputs.
        OutputsAsInputs,

        // In and inout parameters, as outputs.
        InputsAsOutputs,
    }

    static class ProviderSlotUtils
    {
        internal const int kReturnSlotId = 0;

        internal static void Rebuild(AbstractMaterialNode node, IFieldCollection definition, SlotLayout layout = SlotLayout.AsDeclared)
        {
            var previousIds = PreviousSlotIds(node);
            HashSet<int> usedIds = new();
            List<MaterialSlot> slots = new();

            // Return type is a special case.
            if (layout != SlotLayout.InputsAsOutputs && definition is IShaderFunction function && !ShaderObjectUtils.IsVoid(function.ReturnType))
            {
                usedIds.Add(kReturnSlotId);
                var direction = layout == SlotLayout.OutputsAsInputs ? SlotType.Input : SlotType.Output;
                slots.Add(HeaderUtils.MakeSlotFromField(ShaderObjectUtils.MakeReturnField(function, HeaderUtils.kReturnSlotName), kReturnSlotId, direction));
            }

            foreach (var field in definition.Fields)
            {
                if (!previousIds.TryGetValue(field.Name, out var ids))
                    continue;
                var (asInput, asOutput) = Sides(field, layout);
                if (asInput && ids.input > -1)
                    usedIds.Add(ids.input);
                if (asOutput && ids.output > -1)
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

                var (asInput, asOutput) = Sides(field, layout);
                if (asInput)
                {
                    var slot = HeaderUtils.MakeSlotFromField(field, ids.input > -1 ? ids.input : NewId(), SlotType.Input);
                    // Instead of a value, an input that passes the incoming argument shows the parameter's direction.
                    if (field.IsInput && layout is SlotLayout.AsDeclaredPassthrough or SlotLayout.OutputsAsInputs)
                        slot.CustomBinding = field.IsOutput ? "inout" : "in";
                    slots.Add(slot);
                }
                if (asOutput)
                    slots.Add(HeaderUtils.MakeSlotFromField(field, ids.output > -1 ? ids.output : NewId(), SlotType.Output));
            }

            Apply(node, slots);
        }

        static (bool asInput, bool asOutput) Sides(IShaderField field, SlotLayout layout) => layout switch
        {
            SlotLayout.OutputsAsInputs => (field.IsOutput, false),
            SlotLayout.InputsAsOutputs => (false, field.IsInput),
            _ => (field.IsInput, field.IsOutput),
        };

        internal static void Apply(AbstractMaterialNode node, IReadOnlyList<MaterialSlot> slots)
        {
            List<int> order = new();
            foreach (var slot in slots)
            {
                if (slot == null)
                    continue;

                // Space slots are rebuilt rather than updated.
                var kept = node.AddSlot(slot, slot is not SpaceMaterialSlot, slot is not SpaceMaterialSlot);
                // A kept slot doesn't take the new slot's custom binding by itself.
                if (kept != slot && kept.CustomBinding != slot.CustomBinding)
                {
                    kept.CustomBinding = slot.CustomBinding;
                    node.Dirty(ModificationScope.Topological);
                }
                // Outputs hold no user value, so a kept output takes the new default as its value.
                if (kept != slot && kept.isOutputSlot)
                    kept.CopyValuesFrom(slot);
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
