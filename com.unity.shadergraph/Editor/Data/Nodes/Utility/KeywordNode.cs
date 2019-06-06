using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Keyword")]
    class KeywordNode : AbstractMaterialNode, IOnAssetEnabled, IGeneratesBodyCode, IGeneratesBranch
    {
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
                UpdateEnumEntries();
                Dirty(ModificationScope.Topological);
            }
        }

        public override bool canSetPrecision => false;

        // TODO: Set true when correct branch code is generated
        public override bool hasPreview => true;

        public void OnEnable()
        {
            UpdateNode();
            UpdateEnumEntries();
        }

        public const int OutputSlotId = 0;

        public void UpdateNode()
        {
            var keyword = owner.keywords.FirstOrDefault(x => x.guid == keywordGuid);
            if (keyword == null)
                return;
            
            // Set name
            name = keyword.displayName;

            // Boolean type slots
            if(keyword.keywordType == ShaderKeywordType.Boolean)
            {
                AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, "Out", "Out", SlotType.Output, Vector4.zero));
                AddSlot(new DynamicVectorMaterialSlot(1, "On", "On", SlotType.Input, Vector4.zero));
                AddSlot(new DynamicVectorMaterialSlot(2, "Off", "Off", SlotType.Input, Vector4.zero));
                RemoveSlotsNameNotMatching(new int[] {0, 1, 2});
            }
        }

        public void UpdateEnumEntries()
        {
            var keyword = owner.keywords.FirstOrDefault(x => x.guid == keywordGuid);
            if (keyword == null)
                return;

            if(keyword.keywordType != ShaderKeywordType.Enum)
                return;
            
            // Get slots
            List<MaterialSlot> inputSlots = new List<MaterialSlot>();
            GetInputSlots(inputSlots);

            // Store the edges
            Dictionary<MaterialSlot, List<IEdge>> edgeDict = new Dictionary<MaterialSlot, List<IEdge>>();
            foreach (MaterialSlot slot in inputSlots)
                edgeDict.Add(slot, (List<IEdge>)slot.owner.owner.GetEdges(slot.slotReference));
            
            // Remove old slots
            for(int i = 0; i < inputSlots.Count; i++)
                RemoveSlot(inputSlots[i].id);

            // Add output slot
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, "Out", "Out", SlotType.Output, Vector4.zero));

            // Add input slots
            int[] slotIds = new int[keyword.entries.Count + 1];
            slotIds[keyword.entries.Count] = OutputSlotId;
            for(int i = 0; i < keyword.entries.Count; i++)
            {
                MaterialSlot slot = inputSlots.Where(x => x.id == keyword.entries[i].id).FirstOrDefault();
                if(slot == null)
                    slot = new DynamicVectorMaterialSlot(keyword.entries[i].id, keyword.entries[i].displayName, keyword.entries[i].referenceName, SlotType.Input, Vector4.zero);

                AddSlot(slot);
                slotIds[i] = keyword.entries[i].id;
            }
            RemoveSlotsNameNotMatching(slotIds);

            // Reconnect the edges
            foreach (KeyValuePair<MaterialSlot, List<IEdge>> entry in edgeDict)
            {
                foreach (IEdge edge in entry.Value)
                    owner.Connect(edge.outputSlot, edge.inputSlot);
            }

            ValidateNode();
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            var keyword = owner.keywords.FirstOrDefault(x => x.guid == keywordGuid);
            if (keyword == null)
                return;
            
            var outputSlot = FindOutputSlot<MaterialSlot>(OutputSlotId);
            switch(keyword.keywordType)
            {
                case ShaderKeywordType.Boolean:
                    var onValue = GetSlotValue(1, generationMode);
                    var offValue = GetSlotValue(2, generationMode);

                    if(generationMode == GenerationMode.Preview)
                    {
                        sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(OutputSlotId)} = {(keyword.value == 1 ? onValue : offValue)};"));
                        return;
                    }

                    sb.AppendLine($"#ifdef {keyword.referenceName}_ON");
                    using(sb.IndentScope())
                        sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(OutputSlotId)} = {onValue};"));
                    sb.AppendLine($"#else");
                    using(sb.IndentScope())
                        sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(OutputSlotId)} = {offValue};"));
                    sb.AppendLine($"#endif");
                    break;
                case ShaderKeywordType.Enum:
                    if(generationMode == GenerationMode.Preview)
                    {
                        var value = GetSlotValue(keyword.entries[keyword.value].id, generationMode);
                        sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(OutputSlotId)} = {value};"));
                        return;
                    }

                    for(int i = 0; i < keyword.entries.Count; i++)
                    {
                        if(i == 0)
                        {
                            sb.AppendLine($"#if defined({keyword.referenceName}_{keyword.entries[i].referenceName})");
                        }
                        else if(i == keyword.entries.Count - 1)
                        {
                            sb.AppendLine($"#else");
                        }
                        else
                        {
                            sb.AppendLine($"#elif defined({keyword.referenceName}_{keyword.entries[i].referenceName})");
                        }
                        using(sb.IndentScope())
                        {
                            var value = GetSlotValue(keyword.entries[i].id, generationMode);
                            sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(OutputSlotId)} = {value};"));
                        }
                    }
                    sb.AppendLine($"#endif");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void CollectShaderKeywords(KeywordCollector keywords, GenerationMode generationMode)
        {
            if(!generationMode.IsPreview())
                return;

            var keyword = owner.keywords.FirstOrDefault(x => x.guid == keywordGuid);
            if (keyword == null)
                return;

            keywords.AddShaderKeyword(keyword);
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
            m_KeywordGuidSerialized = m_KeywordGuid.ToString();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (!string.IsNullOrEmpty(m_KeywordGuidSerialized))
                m_KeywordGuid = new Guid(m_KeywordGuidSerialized);
        }
    }
}