using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class LegacyNode
    {
        [SerializeField]
        string m_GroupGuidSerialized = default;

        [SerializeField]
        string m_GuidSerialized = default;

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableSlots = default;

        [SerializeField]
        string m_PropertyGuidSerialized = default;

        [SerializeField]
        string m_KeywordGuidSerialized = default;

        public string groupGuid => m_GroupGuidSerialized;

        public string guid => m_GuidSerialized;

        public List<SerializationHelper.JSONSerializedElement> slots => m_SerializableSlots;

        public string propertyGuid => m_PropertyGuidSerialized;

        public string keywordGuid => m_KeywordGuidSerialized;
    }
}
