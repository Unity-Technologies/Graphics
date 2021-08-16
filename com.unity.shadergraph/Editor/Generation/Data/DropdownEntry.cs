using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [GenerationAPI]
    internal struct DropdownEntry
    {
        public int id; // Used to determine what MaterialSlot an entry belongs to
        public string displayName;

        // In this case, we will handle the actual IDs later
        public DropdownEntry(string displayName)
        {
            this.id = -1;
            this.displayName = displayName;
        }

        internal DropdownEntry(int id, string displayName)
        {
            this.id = id;
            this.displayName = displayName;
        }
    }
}
