using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Keyword")]
    class KeywordNode : AbstractMaterialNode, IOnAssetEnabled
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
                Dirty(ModificationScope.Topological);
            }
        }

        public override bool canSetPrecision => false;

        // TODO: Set true when correct branch code is generated
        public override bool hasPreview => false;

        public void OnEnable()
        {
            UpdateNode();
        }

        public const int OutputSlotId = 0;

        public void UpdateNode()
        {
            var keyword = owner.keywords.FirstOrDefault(x => x.guid == keywordGuid);
            if (keyword == null)
                return;
            
            name = keyword.displayName;

            List<MaterialSlot> inputSlots = new List<MaterialSlot>();
            GetInputSlots(inputSlots);
            for(int i = 0; i < inputSlots.Count; i++)
                RemoveSlot(inputSlots[i].id);

            int[] slotIds = new int[keyword.entries.Count + 1];
            slotIds[keyword.entries.Count] = OutputSlotId;

            for(int i = 0; i < keyword.entries.Count; i++)
            {
                int slotId = i + 1;
                AddSlot(new DynamicVectorMaterialSlot(slotId, keyword.entries[i].displayName, keyword.entries[i].referenceName, SlotType.Input, Vector4.zero));
                slotIds[i] = slotId;
            }
            
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, "Out", "Out", SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(slotIds);
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