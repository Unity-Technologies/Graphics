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
        bool m_IsDirty;

        [SerializeField]
        bool m_IsSubGraph;

        [SerializeField]
        string m_AssetGuid;

        [NonSerialized]
        GraphData m_Graph;
        
        [NonSerialized]
        GraphData m_DeserializedGraph;

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

        public void RegisterCompleteObjectUndo(string name)
        {
            Undo.RegisterCompleteObjectUndo(this, name);
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
            var deserializedGraph = SerializationHelper.Deserialize<GraphData>(m_SerializedGraph, GraphUtil.GetLegacyTypeRemapping());
            deserializedGraph.isSubGraph = m_IsSubGraph;
            deserializedGraph.assetGuid = m_AssetGuid;
            if (graph == null)
                graph = deserializedGraph;
            else
                m_DeserializedGraph = deserializedGraph;
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

            Undo.undoRedoPerformed += UndoRedoPerformed;
            UndoRedoPerformed();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        void UndoRedoPerformed()
        {
            if (m_DeserializedGraph != null)
            {
                graph.ReplaceWith(m_DeserializedGraph);
                m_DeserializedGraph = null;
            }
        }
    }
}
