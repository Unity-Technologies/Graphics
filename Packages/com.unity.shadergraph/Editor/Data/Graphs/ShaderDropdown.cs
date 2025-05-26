using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class ShaderDropdown : ShaderInput
    {
        public ShaderDropdown()
        {
            this.displayName = "Dropdown";
            // Add sensible default entries for Enum type
            m_Entries = new List<DropdownEntry>();
            m_Entries.Add(new DropdownEntry(1, "A"));
            m_Entries.Add(new DropdownEntry(2, "B"));
        }

        internal override bool canPromoteToFinalShader => false;

        [SerializeField]
        private List<DropdownEntry> m_Entries;

        public List<DropdownEntry> entries
        {
            get => m_Entries;
            set => m_Entries = value;
        }

        internal override bool isCustomSlotAllowed { get => false; }
        public override bool allowedInMainGraph { get => false; }

        [SerializeField]
        private int m_Value;

        private int GetClampedValue(int value)
        {
            return count > 0 ? Mathf.Clamp(value, 0, count - 1) : 0;
        }

        public int value
        {
            get => GetClampedValue(m_Value);
            set => m_Value = GetClampedValue(value);
        }

        public string entryName
        {
            get => entries[value].displayName;
        }

        public int entryId
        {
            get => entries[value].id;
        }

        public bool ContainsEntry(string entryName)
        {
            return entries.Any(x => x.displayName.Equals(entryName));
        }

        public int IndexOfName(string entryName)
        {
            return entries.FindIndex((DropdownEntry entry) => entry.displayName.Equals(entryName));
        }

        public int IndexOfId(int entryId)
        {
            return entries.FindIndex((DropdownEntry entry) => entry.id.Equals(entryId));
        }

        public int count
        {
            get { return m_Entries.Count; }
        }

        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal override ConcreteSlotValueType concreteShaderValueType => ConcreteSlotValueType.Vector1;

        internal override ShaderInput Copy()
        {
            return new ShaderDropdown()
            {
                displayName = displayName,
                value = value,
                entries = entries,
            };
        }

        public override int latestVersion => 0;
    }
}
