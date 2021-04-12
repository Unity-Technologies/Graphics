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

        [SerializeField]
        private List<DropdownEntry> m_Entries;

        public List<DropdownEntry> entries
        {
            get => m_Entries;
            set => m_Entries = value;
        }

        [SerializeField]
        private int m_Value;

        public int value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public string entryName
        {
            get => entries[value].displayName;
        }

        public bool ContainsEntry(string entryName)
        {
            return entries.Any(x => x.displayName.Equals(entryName));
        }

        public int IndexOf(string entryName)
        {
            for (var index = 0; index < entries.Count; ++index)
            {
                if (entries[index].displayName.Equals(entryName))
                    return index;
            }
            return -1;
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
