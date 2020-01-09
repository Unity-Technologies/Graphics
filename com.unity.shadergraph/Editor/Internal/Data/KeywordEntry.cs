using System;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public struct KeywordEntry
    {
        public string displayName;
        public string referenceName;

        internal KeywordEntry(int id, string displayName, string referenceName)
        {
            this.displayName = displayName;
            this.referenceName = referenceName;
        }
    }
}
