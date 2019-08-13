using System;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    struct KeywordEntry
    {
        public int id;
        public string displayName;
        public string referenceName;

        public KeywordEntry(int id, string displayName, string referenceName)
        {
            this.id = id;
            this.displayName = displayName;
            this.referenceName = referenceName;
        }
    }
}
