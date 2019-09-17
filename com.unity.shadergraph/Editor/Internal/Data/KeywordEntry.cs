using System;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public struct KeywordEntry
    {
        public int id;
        public string displayName;
        public string referenceName;

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
