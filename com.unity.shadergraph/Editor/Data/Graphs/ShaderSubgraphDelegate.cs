using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class ShaderSubgraphDelegate : ShaderInput
    {
        public ShaderSubgraphDelegate()
        {
            this.displayName = "Subgraph Delegate";
            
            // Add sensible default entries
            m_Input_Entries = new List<SubgraphDelegateEntry>();
            m_Input_Entries.Add(new SubgraphDelegateEntry(1, PropertyType.Vector1, "Vector1In", "_VECTOR1IN"));
            m_Output_Entries = new List<SubgraphDelegateEntry>();
            m_Output_Entries.Add(new SubgraphDelegateEntry(1, PropertyType.Vector1, "Vector1Out", "_VECTOR1OUT"));
        }

        [SerializeField]
        private List<SubgraphDelegateEntry> m_Input_Entries;
        [SerializeField]
        private List<SubgraphDelegateEntry> m_Output_Entries;
        public SubGraphNode connectedNode;

        public List<SubgraphDelegateEntry> input_Entries
        {
            get => m_Input_Entries;
            set => m_Input_Entries = value;
        }

        public List<SubgraphDelegateEntry> output_Entries
        {
            get => m_Output_Entries;
            set => m_Output_Entries = value;
        }

        [SerializeField]
        private bool m_IsEditable = true;

        public bool isEditable
        {
            get => m_IsEditable;
            set => m_IsEditable = value;
        }

        [SerializeField]
        private bool m_IsExposable = true;

        internal override bool isExposable => m_IsExposable;

        internal override bool isRenamable => isEditable;

        internal override ConcreteSlotValueType concreteShaderValueType => m_Input_Entries.Count > 0 ? m_Output_Entries[0].propertyType.ToConcreteShaderValueType() : ConcreteSlotValueType.Vector1;

        public override string GetDefaultReferenceName()
        {
            // _ON suffix is required for exposing Boolean type to Material
            var suffix = string.Empty;

            return $"_{GuidEncoder.Encode(guid)}{suffix}".ToUpper();
        }

        internal override ShaderInput Copy()
        {
            // Keywords copy reference name
            // This is because keywords are copied between graphs
            // When copying dependent nodes
            return new ShaderSubgraphDelegate()
            {
                displayName = displayName,
                overrideReferenceName = overrideReferenceName,
                generatePropertyBlock = generatePropertyBlock,
                m_IsExposable = isExposable,
                isEditable = isEditable,
                input_Entries = input_Entries,
                output_Entries = output_Entries
            };
        }
    }
}
