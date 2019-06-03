using System;
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
                var keyword = owner.keywords.FirstOrDefault(x => x.guid == value);
                if (keyword == null)
                    return;
                
                UpdateNode(keyword);
                Dirty(ModificationScope.Topological);
            }
        }
        public override bool canSetPrecision => false;

        // TODO: Set true when correct branch code is generated
        public override bool hasPreview => false;

        public void OnEnable()
        {
            var keyword = owner.keywords.FirstOrDefault(x => x.guid == keywordGuid);
            if (keyword == null)
                return;
            
            UpdateNode(keyword);
        }

        public const int OutputSlotId = 0;

        private void UpdateNode(ShaderKeyword keyword)
        {
            name = keyword.displayName;

            int[] slotIds = new int[keyword.entries.Count + 1];
            slotIds[keyword.entries.Count] = OutputSlotId;

            for(int i = 0; i < keyword.entries.Count; i++)
            {
                int slotId = i + 1;
                AddSlot(new DynamicVectorMaterialSlot(slotId, keyword.entries[i].Key, keyword.entries[i].Value, SlotType.Input, Vector4.zero));
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