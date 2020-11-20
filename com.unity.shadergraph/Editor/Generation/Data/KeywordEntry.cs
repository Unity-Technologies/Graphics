using System;

namespace UnityEditor.ShaderGraph
{
    [Serializable][GenerationAPI]
    internal struct KeywordEntry
    {
        public int id; // Used to determine what MaterialSlot an entry belongs to
        public string displayName;
        public string referenceName;

        // In this case, we will handle the actual IDs later
        public KeywordEntry(string displayName, string referenceName)
        {
            this.id = -1;
            this.displayName = displayName;
            this.referenceName = referenceName;
        }

        internal KeywordEntry(int id, string displayName, string referenceName)
        {
            this.id = id;
            this.displayName = displayName;
            this.referenceName = referenceName;
        }
    }
}
