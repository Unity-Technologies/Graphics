using System;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Graphing
{
    class GraphObject : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        SerializationHelper.JSONSerializedElement m_SerializedGraph;

        [SerializeField]
        int m_SerializedVersion;

        [SerializeField]
        bool m_IsDirty;

        [SerializeField]
        bool m_IsSubGraph;

        [SerializeField]
        string m_AssetGuid;

        [NonSerialized]
        GraphData m_Graph;

        [NonSerialized]
        int m_DeserializedVersion;

        public GraphData graph
        {
            get { return m_Graph; }
            set
            {
                if (m_Graph != null)
                    m_Graph.owner = null;
                m_Graph = value;
                if (m_Graph != null)
                    m_Graph.owner = this;
            }
        }

        public bool isDirty
        {
            get { return m_IsDirty; }
            set { m_IsDirty = value; }
        }

        public void RegisterCompleteObjectUndo(string actionName)
        {
            Undo.RegisterCompleteObjectUndo(this, actionName);
            m_SerializedVersion++;
            m_DeserializedVersion++;
            m_IsDirty = true;
        }

        public void OnBeforeSerialize()
        {
            if (graph != null)
            {
                m_SerializedGraph = SerializationHelper.Serialize(graph);
                m_IsSubGraph = graph.isSubGraph;
                m_AssetGuid = graph.assetGuid;
            }
        }

        public void OnAfterDeserialize()
        {
            if (graph == null)
            {
                graph = DeserializeGraph();
            }
        }

        public bool wasUndoRedoPerformed => m_DeserializedVersion != m_SerializedVersion;

        public void HandleUndoRedo()
        {
            Debug.Assert(wasUndoRedoPerformed);
            var deserializedGraph = DeserializeGraph();
            m_Graph.ReplaceWith(deserializedGraph);
        }

        GraphData DeserializeGraph()
        {
            var deserializedGraph = SerializationHelper.Deserialize<GraphData>(m_SerializedGraph, GraphUtil.GetLegacyTypeRemapping());
            deserializedGraph.isSubGraph = m_IsSubGraph;
            deserializedGraph.assetGuid = m_AssetGuid;
            m_DeserializedVersion = m_SerializedVersion;
            m_SerializedGraph = default(SerializationHelper.JSONSerializedElement);
            return deserializedGraph;
        }

        public void Validate()
        {
            if (graph != null)
            {
                graph.OnEnable();
                graph.ValidateGraph();
            }
        }

        void OnEnable()
        {
            Validate();
        }

        void OnDestroy()
        {
            graph?.OnDisable();
        }
    }
}
