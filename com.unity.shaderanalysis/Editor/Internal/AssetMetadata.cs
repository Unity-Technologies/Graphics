using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis.Internal
{
    public class AssetMetadata : ScriptableObject, ISerializationCallbackReceiver
    {
        [Serializable]
        class ReportEntry
        {
            [SerializeField]
            internal string guid;
            [SerializeField]
            internal ShaderBuildReport report;
        }

        [SerializeField]
        List<ReportEntry> m_Entries = new List<ReportEntry>();
        [SerializeField]
        BuildTarget m_Target;
        public BuildTarget target { get { return m_Target; } set { m_Target = value; } }

        Dictionary<string, int> m_GUIDIndex = new Dictionary<string, int>();

        public ShaderBuildReport GetReport(string guid)
        {
            int index;
            return m_GUIDIndex.TryGetValue(guid, out index)
                ? m_Entries[index].report
                : null;
        }

        public void SetReport(string guid, ShaderBuildReport report)
        {
            int index;
            if (m_GUIDIndex.TryGetValue(guid, out index))
                m_Entries[index].report = report;
            else
            {
                m_Entries.Add(new ReportEntry { report = report, guid = guid });
                m_GUIDIndex[guid] = m_Entries.Count - 1;
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (m_Entries != null)
            {
                for (var i = 0; i < m_Entries.Count; i++)
                    m_GUIDIndex[m_Entries[i].guid] = i;
            }
        }
    }
}
