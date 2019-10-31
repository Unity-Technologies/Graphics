using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class GraphDataV0
    {
        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedProperties = default;

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedKeywords = default;

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = default;

        [SerializeField]
        List<GroupDataV0> m_Groups = default;

        [SerializeField]
        List<StickyNoteDataV0> m_StickyNotes = default;

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableEdges = default;

        [SerializeField]
        string m_ActiveOutputNodeGuidSerialized = default;

        public List<SerializationHelper.JSONSerializedElement> edges => m_SerializableEdges;

        public string activeOutputNodeGuid => m_ActiveOutputNodeGuidSerialized;

        public List<SerializationHelper.JSONSerializedElement> properties => m_SerializedProperties;

        public List<SerializationHelper.JSONSerializedElement> keywords => m_SerializedKeywords;

        public List<GroupDataV0> groups => m_Groups;

        public List<SerializationHelper.JSONSerializedElement> nodes => m_SerializableNodes;

        public List<StickyNoteDataV0> stickyNotes => m_StickyNotes;
    }
}
