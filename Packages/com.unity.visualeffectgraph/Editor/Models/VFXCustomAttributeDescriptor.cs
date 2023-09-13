using System;
using System.Collections.Generic;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX
{
    interface IVFXAttributeUsage
    {
        IEnumerable<VFXAttribute> usedAttributes { get; }
        void Rename(string oldName, string newName);
    }

    [Serializable]
    class VFXCustomAttributeDescriptor : VFXObject
    {
        private List<string> m_UsedBySubgraphs;

        [SerializeField]
        private string m_Description;

        [SerializeField]
        private string m_AttributeName;

        [SerializeField]
        private CustomAttributeUtility.Signature m_Type;

        [SerializeField]
        private bool m_IsExpanded;

        public VFXGraph graph { get; set; }

        public bool isReadOnly { get; set; }
        public IEnumerable<string> usedInSubgraphs => m_UsedBySubgraphs;

        public string attributeName
        {
            get => m_AttributeName;
            set => m_AttributeName = value;
        }

        public CustomAttributeUtility.Signature type
        {
            get => m_Type;
            set => m_Type = value;
        }

        public string description
        {
            get => m_Description;
            set => m_Description = value;
        }

        public bool isExpanded
        {
            get => this.m_IsExpanded;
            set => this.m_IsExpanded = value;
        }

        public void AddSubgraphUse(string subgraphName)
        {
            if (this.m_UsedBySubgraphs == null)
            {
                this.m_UsedBySubgraphs = new List<string> { subgraphName };
            }
            else if (!this.m_UsedBySubgraphs.Contains(subgraphName))
            {
                this.m_UsedBySubgraphs.Add(subgraphName);
            }
        }

        public void RemoveSubgraphUse(string subgraphName)
        {
            if (m_UsedBySubgraphs?.Count > 0)
            {
                m_UsedBySubgraphs.Remove(subgraphName);
            }
        }

        public void ClearSubgraphUse() => m_UsedBySubgraphs = null;

        public bool Changed(string oldName, string newName, CustomAttributeUtility.Signature newType, string newDescription)
        {
            if (string.Compare(oldName, newName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return this.graph.TryRenameCustomAttribute(oldName, newName);
            }

            this.type = newType;
            this.description = newDescription;
            return this.graph.TryUpdateCustomAttribute(this.attributeName, this.type, this.description);
        }
    }
}
