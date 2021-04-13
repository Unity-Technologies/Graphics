using System;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
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

        // this value stores whether an undo operation has been registered (which indicates a change has been made to the graph)
        // and is used to trigger the MaterialGraphEditWindow to update it's title
        public bool isDirty
        {
            get { return m_IsDirty; }
            set { m_IsDirty = value; }
        }

        public virtual void RegisterCompleteObjectUndo(string actionName)
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
                var json = MultiJson.Serialize(graph);
                m_SerializedGraph = new SerializationHelper.JSONSerializedElement { JSONnodeData = json };
                m_IsSubGraph = graph.isSubGraph;
                m_AssetGuid = graph.assetGuid;
            }
        }

        public void OnAfterDeserialize()
        {
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
            var json = m_SerializedGraph.JSONnodeData;
            var deserializedGraph = new GraphData {isSubGraph = m_IsSubGraph, assetGuid = m_AssetGuid};
            MultiJson.Deserialize(deserializedGraph, json);
            m_DeserializedVersion = m_SerializedVersion;
            m_SerializedGraph = default;
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
            if (graph == null && m_SerializedGraph.JSONnodeData != null)
            {
                graph = DeserializeGraph();
            }
            Validate();
        }

        void OnDestroy()
        {
            graph?.OnDisable();
        }
    }
}
