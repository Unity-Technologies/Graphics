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
            this.displayName = ConcreteSlotValueType.Vector1.ToString();
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
