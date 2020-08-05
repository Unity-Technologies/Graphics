using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Serialization;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
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
        private string m_KeywordGuidSerialized;

        private Guid m_KeywordGuid;

        public Guid keywordGuid
        {
            get { return m_KeywordGuid; }
            set
            {
                if (m_KeywordGuid == value)
                    return;

                m_KeywordGuid = value;
                UpdateNode();
                Dirty(ModificationScope.Topological);
            }
        }

        public override bool canSetPrecision => false;
        public override bool hasPreview => true;
        public const int OutputSlotId = 0;

        public void OnEnable()
        {
            UpdateNode();
        }

        public void UpdateNode()
        {
            var keyword = owner.keywords.FirstOrDefault(x => x.guid == keywordGuid);
            if (keyword == null)
                return;
            
            name = keyword.displayName;
            UpdatePorts(keyword);
        }

        void UpdatePorts(ShaderKeyword keyword)
        {
            switch(keyword.keywordType)
            {
                case KeywordType.Boolean:
                {
                    // Boolean type has preset slots
                    AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, "Out", "Out", SlotType.Output, Vector4.zero));
                    AddSlot(new DynamicVectorMaterialSlot(1, "On", "On", SlotType.Input, Vector4.zero));
                    AddSlot(new DynamicVectorMaterialSlot(2, "Off", "Off", SlotType.Input, Vector4.zero));
                    RemoveSlotsNameNotMatching(new int[] {0, 1, 2});
                    break;
                }
                case KeywordType.Enum:
                {
                    // Get slots
                    List<MaterialSlot> inputSlots = new List<MaterialSlot>();
                    GetInputSlots(inputSlots);

                    // Store the edges
                    Dictionary<MaterialSlot, List<IEdge>> edgeDict = new Dictionary<MaterialSlot, List<IEdge>>();
                    foreach (MaterialSlot slot in inputSlots)
                        edgeDict.Add(slot, (List<IEdge>)slot.owner.owner.GetEdges(slot.slotReference));
                    
                    // Remove old slots
                    for(int i = 0; i < inputSlots.Count; i++)
                    {
                        RemoveSlot(inputSlots[i].id);
                    } 

                    // Add output slot
                    AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, "Out", "Out", SlotType.Output, Vector4.zero));

                    // Add input slots
                    int[] slotIds = new int[keyword.entries.Count + 1];
                    slotIds[keyword.entries.Count] = OutputSlotId;
                    for(int i = 0; i < keyword.entries.Count; i++)
                    {
                        // Get slot based on entry id
                        MaterialSlot slot = inputSlots.Where(x => 
                            x.id == keyword.entries[i].id &&
                            x.RawDisplayName() == keyword.entries[i].displayName && 
                            x.shaderOutputName == keyword.entries[i].referenceName).FirstOrDefault();

                        // If slot doesnt exist its new so create it
                        if(slot == null)
                        {
                            slot = new DynamicVectorMaterialSlot(keyword.entries[i].id, keyword.entries[i].displayName, keyword.entries[i].referenceName, SlotType.Input, Vector4.zero);
                        }

                        AddSlot(slot);
                        slotIds[i] = keyword.entries[i].id;
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
                    break;
                }
            }

            ValidateNode();
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var keyword = owner.keywords.FirstOrDefault(x => x.guid == keywordGuid);
            if (keyword == null)
                return;
            
            var outputSlot = FindOutputSlot<MaterialSlot>(OutputSlotId);
            switch(keyword.keywordType)
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
                    for(int i = 0; i < keyword.entries.Count; i++)
                    {
                        // Insert conditional
                        if(i == 0)
                        {
                            sb.AppendLine($"#if defined({keyword.referenceName}_{keyword.entries[i].referenceName})");
                        }
                        else if(i == keyword.entries.Count - 1)
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
            switch(permutation.Key.keywordType)
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

        protected override bool CalculateNodeHasError(ref string errorMessage)
        {
            if (!keywordGuid.Equals(Guid.Empty) && !owner.keywords.Any(x => x.guid == keywordGuid))
                return true;

            return false;
        }
        
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            // Handle keyword guid serialization
            m_KeywordGuidSerialized = m_KeywordGuid.ToString();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            // Handle keyword guid serialization
            if (!string.IsNullOrEmpty(m_KeywordGuidSerialized))
            {
                m_KeywordGuid = new Guid(m_KeywordGuidSerialized);
            } 
        }
    }
}
