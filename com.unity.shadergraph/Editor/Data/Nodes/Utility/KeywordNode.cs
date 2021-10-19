using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Utility", "Keyword")]
    class KeywordNode : AbstractMaterialNode, IOnAssetEnabled, IGeneratesBodyCode
    {
        internal const int k_MinEnumEntries = 2;
        internal const int k_MaxEnumEntries = 8;

        public KeywordNode()
        {
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        JsonRef<ShaderKeyword> m_Keyword;

        public ShaderKeyword keyword
        {
            get { return m_Keyword; }
            set
            {
                if (m_Keyword == value)
                    return;

                m_Keyword = value;
                m_Keyword.value.displayNameUpdateTrigger += UpdateNodeDisplayName;
                UpdateNode();
                Dirty(ModificationScope.Topological);
            }
        }

        public override bool canSetPrecision => false;
        public override bool hasPreview => true;
        public const int OutputSlotId = 0;

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
            name = keyword.displayName;
            UpdatePorts();
        }

        void UpdatePorts()
        {
            switch (keyword.keywordType)
            {
                case KeywordType.Boolean:
                {
                    // Boolean type has preset slots
                    PooledList<MaterialSlot> temp = PooledList<MaterialSlot>.Get();
                    GetInputSlots(temp);
                    if (temp.Any())
                    {
                        temp.Dispose();
                        break;
                    }
                    else
                    {
                        temp.Dispose();
                    }
                    AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, "Out", "Out", SlotType.Output, Vector4.zero));
                    AddSlot(new DynamicVectorMaterialSlot(1, "On", "On", SlotType.Input, Vector4.zero));
                    AddSlot(new DynamicVectorMaterialSlot(2, "Off", "Off", SlotType.Input, Vector4.zero));
                    RemoveSlotsNameNotMatching(new int[] { 0, 1, 2 });
                    break;
                }
                case KeywordType.Enum:
                    using (var inputSlots = PooledList<MaterialSlot>.Get())
                    using (var slotIDs = PooledList<int>.Get())
                    {
                        // Get slots
                        GetInputSlots(inputSlots);


                        // Add output slot
                        AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, "Out", "Out", SlotType.Output, Vector4.zero));
                        slotIDs.Add(OutputSlotId);

                        // Add input slots
                        for (int i = 0; i < keyword.entries.Count; i++)
                        {
                            // Get slot based on entry id
                            MaterialSlot slot = inputSlots.Find(x =>
                                x.id == keyword.entries[i].id &&
                                x.RawDisplayName() == keyword.entries[i].displayName &&
                                x.shaderOutputName == keyword.entries[i].referenceName);

                            // If slot doesn't exist, it's new so create it
                            if (slot == null)
                            {
                                slot = new DynamicVectorMaterialSlot(keyword.entries[i].id, keyword.entries[i].displayName, keyword.entries[i].referenceName, SlotType.Input, Vector4.zero);
                            }

                            AddSlot(slot);
                            slotIDs.Add(keyword.entries[i].id);
                        }
                        RemoveSlotsNameNotMatching(slotIDs);
                        bool orderChanged = SetSlotOrder(slotIDs);
                        if (orderChanged)
                        {
                            // unfortunately there is no way to get the view to update slot order other than Topological
                            Dirty(ModificationScope.Topological);
                        }
                        break;
                    }
            }

            ValidateNode();
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot<MaterialSlot>(OutputSlotId);
            switch (keyword.keywordType)
            {
                case KeywordType.Boolean:
                {
                    // Get values
                    var onValue = GetSlotValue(1, generationMode);
                    var offValue = GetSlotValue(2, generationMode);

                    // Append code
                    sb.AppendLine($"#if defined({keyword.referenceName})");
                    sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(OutputSlotId)} = {onValue};"));
                    sb.AppendLine("#else");
                    sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(OutputSlotId)} = {offValue};"));
                    sb.AppendLine("#endif");
                    break;
                }
                case KeywordType.Enum:
                {
                    // Iterate all entries in the keyword
                    for (int i = 0; i < keyword.entries.Count; i++)
                    {
                        // Insert conditional
                        if (i == 0)
                        {
                            sb.AppendLine($"#if defined({keyword.referenceName}_{keyword.entries[i].referenceName})");
                        }
                        else if (i == keyword.entries.Count - 1)
                        {
                            sb.AppendLine("#else");
                        }
                        else
                        {
                            sb.AppendLine($"#elif defined({keyword.referenceName}_{keyword.entries[i].referenceName})");
                        }

                        // Append per-slot code
                        var value = GetSlotValue(GetSlotIdForPermutation(new KeyValuePair<ShaderKeyword, int>(keyword, i)), generationMode);
                        sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(OutputSlotId)} = {value};"));
                    }

                    // End condition
                    sb.AppendLine("#endif");
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public int GetSlotIdForPermutation(KeyValuePair<ShaderKeyword, int> permutation)
        {
            switch (permutation.Key.keywordType)
            {
                // Slot 0 is output
                case KeywordType.Boolean:
                    return 1 + permutation.Value;
                // Ids are stored manually as slots are added
                case KeywordType.Enum:
                    return permutation.Key.entries[permutation.Value].id;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void CalculateNodeHasError()
        {
            if (keyword == null || !owner.keywords.Any(x => x == keyword))
            {
                owner.AddConcretizationError(objectId, "Keyword Node has no associated keyword.");
                hasError = true;
            }
        }
    }
}
