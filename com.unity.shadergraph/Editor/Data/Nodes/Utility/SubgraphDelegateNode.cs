using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Serialization;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Subgraph Delegate")]
    class SubgraphDelegateNode : AbstractMaterialNode, IOnAssetEnabled, IGeneratesBodyCode
    {
        public SubgraphDelegateNode()
        {
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private string m_SubgraphDelegateGuidSerialized;

        private Guid m_SubgraphDelegateGuid;

        public Guid subgraphDelegateGuid
        {
            get { return m_SubgraphDelegateGuid; }
            set
            {
                if (m_SubgraphDelegateGuid == value)
                    return;

                m_SubgraphDelegateGuid = value;
                UpdateNode();
                Dirty(ModificationScope.Topological);
            }
        }

        public override bool canSetPrecision => false;
        public override bool hasPreview => true;
        public const int OutputSlotId = 0;

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
        }

        public void OnEnable()
        {
            Debug.Log("OnEnableTest");
            UpdateNode();
        }

        public void UpdateNode()
        {
            Debug.Log("OnUpdateTest");
            //TODO: Get the subgraph delegate from the graphdata
            var subDelegate = new ShaderSubgraphDelegate();
            name = "Subgraph Delegate";
            UpdatePorts(subDelegate);
        }

        void UpdatePorts(ShaderSubgraphDelegate subDelegate)
        {
            Debug.Log("UpdatePortsTest");
            // Get slots
            List<MaterialSlot> inputSlots = new List<MaterialSlot>();
            GetInputSlots(inputSlots);

            // Store the edges
            Dictionary<MaterialSlot, List<IEdge>> inputEdgeDict = new Dictionary<MaterialSlot, List<IEdge>>();
            foreach (MaterialSlot slot in inputSlots)
                inputEdgeDict.Add(slot, (List<IEdge>)slot.owner.owner.GetEdges(slot.slotReference));

            // Remove old slots
            for (int i = 0; i < inputSlots.Count; i++)
            {
                RemoveSlot(inputSlots[i].id);
            }


            // Get slots
            List<MaterialSlot> outputSlots = new List<MaterialSlot>();
            GetInputSlots(outputSlots);

            // Store the edges
            Dictionary<MaterialSlot, List<IEdge>> outputEdgeDict = new Dictionary<MaterialSlot, List<IEdge>>();
            foreach (MaterialSlot slot in outputSlots)
                outputEdgeDict.Add(slot, (List<IEdge>)slot.owner.owner.GetEdges(slot.slotReference));

            // Remove old slots
            for (int i = 0; i < outputSlots.Count; i++)
            {
                RemoveSlot(outputSlots[i].id);
            }

            // Add input slots
            int[] slotIds = new int[subDelegate.input_Entries.Count + subDelegate.output_Entries.Count];
            for (int i = 0; i < subDelegate.input_Entries.Count; i++)
            {
                // Get slot based on entry id
                MaterialSlot slot = inputSlots.Where(x =>
                x.id == subDelegate.input_Entries[i].id &&
                x.RawDisplayName() == subDelegate.input_Entries[i].displayName &&
                x.shaderOutputName == subDelegate.input_Entries[i].referenceName).FirstOrDefault();

                // If slot doesnt exist its new so create it
                if (slot == null)
                {
                    slot = new DynamicVectorMaterialSlot(subDelegate.input_Entries[i].id, subDelegate.input_Entries[i].displayName, subDelegate.input_Entries[i].referenceName, SlotType.Input, Vector4.zero);
                }

                AddSlot(slot);
                slotIds[i] = subDelegate.input_Entries[i].id;
            }
            for (int i = 0; i < subDelegate.output_Entries.Count; i++)
            {
                // Get slot based on entry id
                MaterialSlot slot = outputSlots.Where(x =>
                x.id == subDelegate.output_Entries[i].id &&
                x.RawDisplayName() == subDelegate.output_Entries[i].displayName &&
                x.shaderOutputName == subDelegate.output_Entries[i].referenceName).FirstOrDefault();

                // If slot doesnt exist its new so create it
                if (slot == null)
                {
                    slot = new DynamicVectorMaterialSlot(subDelegate.output_Entries[i].id, subDelegate.output_Entries[i].displayName, subDelegate.output_Entries[i].referenceName, SlotType.Output, Vector4.zero);
                }

                AddSlot(slot);
                slotIds[i] = subDelegate.output_Entries[i].id;
            }
                RemoveSlotsNameNotMatching(slotIds);

            // Reconnect the edges
            foreach (KeyValuePair<MaterialSlot, List<IEdge>> entry in inputEdgeDict)
            {
                foreach (IEdge edge in entry.Value)
                {
                    owner.Connect(edge.outputSlot, edge.inputSlot);
                }
            }

            // Reconnect the edges
            foreach (KeyValuePair<MaterialSlot, List<IEdge>> entry in outputEdgeDict)
            {
                foreach (IEdge edge in entry.Value)
                {
                    owner.Connect(edge.outputSlot, edge.inputSlot);
                }
            }

            ValidateNode();
        }

        protected override bool CalculateNodeHasError(ref string errorMessage)
        {
            return false;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            // Handle keyword guid serialization
            m_SubgraphDelegateGuidSerialized = m_SubgraphDelegateGuid.ToString();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            // Handle keyword guid serialization
            if (!string.IsNullOrEmpty(m_SubgraphDelegateGuidSerialized))
            {
                m_SubgraphDelegateGuid = new Guid(m_SubgraphDelegateGuidSerialized);
            }
        }
    }
}
