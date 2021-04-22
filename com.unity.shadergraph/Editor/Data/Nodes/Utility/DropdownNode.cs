using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Utility", "Dropdown")]
    class DropdownNode : AbstractMaterialNode, IOnAssetEnabled, IGeneratesBodyCode
    {
        internal const int k_MinEnumEntries = 2;

        public DropdownNode()
        {
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        JsonRef<ShaderDropdown> m_Dropdown;

        public ShaderDropdown dropdown
        {
            get { return m_Dropdown; }
            set
            {
                if (m_Dropdown == value)
                    return;

                m_Dropdown = value;
                m_Dropdown.value.displayNameUpdateTrigger += UpdateNodeDisplayName;
                UpdateNode();
                Dirty(ModificationScope.Topological);
            }
        }

        public override bool canSetPrecision => false;
        public override bool hasPreview => true;
        public const int OutputSlotId = 0;

        public override bool allowedInMainGraph { get => false; }

        public void UpdateNodeDisplayName(string newDisplayName)
        {
            MaterialSlot foundSlot = FindSlot<MaterialSlot>(OutputSlotId);

            if (foundSlot != null)
                foundSlot.displayName = newDisplayName;
        }

        public void OnEnable()
        {
            UpdateNode();
        }

        public void UpdateNode()
        {
            name = dropdown.displayName;
            UpdatePorts();
        }

        void UpdatePorts()
        {
            // Get slots
            List<MaterialSlot> inputSlots = new List<MaterialSlot>();
            GetInputSlots(inputSlots);

            // Store the edges
            Dictionary<MaterialSlot, List<IEdge>> edgeDict = new Dictionary<MaterialSlot, List<IEdge>>();
            foreach (MaterialSlot slot in inputSlots)
                edgeDict.Add(slot, (List<IEdge>)slot.owner.owner.GetEdges(slot.slotReference));

            // Remove old slots
            for (int i = 0; i < inputSlots.Count; i++)
            {
                RemoveSlot(inputSlots[i].id);
            }

            // Add output slot
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, "Out", "Out", SlotType.Output, Vector4.zero));

            // Add input slots
            int[] slotIds = new int[dropdown.entries.Count + 1];
            slotIds[dropdown.entries.Count] = OutputSlotId;
            for (int i = 0; i < dropdown.entries.Count; i++)
            {
                // Get slot based on entry id
                MaterialSlot slot = inputSlots.Where(x =>
                    x.id == dropdown.entries[i].id &&
                    x.RawDisplayName() == dropdown.entries[i].displayName &&
                    x.shaderOutputName == dropdown.entries[i].displayName).FirstOrDefault();

                if (slot == null)
                {
                    slot = new DynamicVectorMaterialSlot(dropdown.entries[i].id, dropdown.entries[i].displayName, dropdown.entries[i].displayName, SlotType.Input, Vector4.zero);
                }

                AddSlot(slot);
                slotIds[i] = dropdown.entries[i].id;
            }
            RemoveSlotsNameNotMatching(slotIds);

            // Reconnect the edges
            foreach (KeyValuePair<MaterialSlot, List<IEdge>> entry in edgeDict)
            {
                foreach (IEdge edge in entry.Value)
                {
                    owner.Connect(edge.outputSlot, edge.inputSlot);
                }
            }

            ValidateNode();
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot<MaterialSlot>(OutputSlotId);

            bool isGeneratingSubgraph = owner.isSubGraph && (generationMode != GenerationMode.Preview);
            if (generationMode == GenerationMode.Preview || !isGeneratingSubgraph)
            {
                sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(OutputSlotId)};"));
                var value = GetSlotValue(GetSlotIdForActiveSelection(), generationMode);
                sb.AppendLine(string.Format($"{GetVariableNameForSlot(OutputSlotId)} = {value};"));
            }
            else
            {
                // Iterate all entries in the dropdown
                for (int i = 0; i < dropdown.entries.Count; i++)
                {
                    if (i == 0)
                    {
                        sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(OutputSlotId)};"));
                        sb.AppendLine($"if ({m_Dropdown.value.referenceName} == {i})");
                    }
                    else
                    {
                        sb.AppendLine($"else if ({m_Dropdown.value.referenceName} == {i})");
                    }

                    {
                        sb.AppendLine("{");
                        sb.IncreaseIndent();
                        var value = GetSlotValue(GetSlotIdForPermutation(new KeyValuePair<ShaderDropdown, int>(dropdown, i)), generationMode);
                        sb.AppendLine(string.Format($"{GetVariableNameForSlot(OutputSlotId)} = {value};"));
                        sb.DecreaseIndent();
                        sb.AppendLine("}");
                    }

                    if (i == dropdown.entries.Count - 1)
                    {
                        sb.AppendLine($"else");
                        sb.AppendLine("{");
                        sb.IncreaseIndent();
                        var value = GetSlotValue(GetSlotIdForPermutation(new KeyValuePair<ShaderDropdown, int>(dropdown, 0)), generationMode);
                        sb.AppendLine(string.Format($"{GetVariableNameForSlot(OutputSlotId)} = {value};"));
                        sb.DecreaseIndent();
                        sb.AppendLine("}");
                    }
                }
            }
        }

        public int GetSlotIdForPermutation(KeyValuePair<ShaderDropdown, int> permutation)
        {
            return permutation.Key.entries[permutation.Value].id;
        }

        public int GetSlotIdForActiveSelection()
        {
            return dropdown.entries[dropdown.value].id;
        }

        protected override void CalculateNodeHasError()
        {
            if (dropdown == null || !owner.dropdowns.Any(x => x == dropdown))
            {
                owner.AddConcretizationError(objectId, "Dropdown Node has no associated dropdown.");
                hasError = true;
            }
        }
    }
}
