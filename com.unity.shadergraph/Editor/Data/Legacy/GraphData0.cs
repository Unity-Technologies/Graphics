using System;
using System.Collections.Generic;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class GraphData0
    {
        public List<SerializationHelper.JSONSerializedElement> m_SerializableNodes;

        public List<SerializationHelper.JSONSerializedElement> m_SerializableEdges;

        public string m_ActiveOutputNodeGuidSerialized;

        public List<SerializationHelper.JSONSerializedElement> m_SerializedProperties;

        public List<SerializationHelper.JSONSerializedElement> m_SerializedKeywords;

        public List<StickyNoteData0> m_StickyNotes;

        public List<GroupData0> m_Groups;

        public int m_Version = -1;
    }
}
