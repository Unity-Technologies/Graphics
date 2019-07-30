using System;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    struct ShaderKeywordEntry
    {
        public int id;
        public string displayName;
        public string referenceName;

        public ShaderKeywordEntry(int id, string displayName, string referenceName)
        {
            this.id = id;
            this.displayName = displayName;
            this.referenceName = referenceName;
        }
    }
}
