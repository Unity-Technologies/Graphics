//using System;
//using Newtonsoft.Json;
//using UnityEditor.ShaderGraph;
//using UnityEditor.ShaderGraph.Serialization;
//using UnityEngine;
//
//namespace UnityEditor.Graphing
//{
//    class GraphObject : ScriptableObject, ISerializationCallbackReceiver
//    {
//        [SerializeField]
//        SerializationHelper.JSONSerializedElement m_SerializedGraph;
//
//        [SerializeField]
//        int m_SerializedVersion;
//
//        [SerializeField]
//        bool m_IsDirty;
//
//        [SerializeField]
//        bool m_IsSubGraph;
//
//        [SerializeField]
//        string m_AssetGuid;
//
//        [NonSerialized]
//        GraphData m_Graph;
//
//        public JsonStore jsonStore { get; set; }
//
//        public int version { get; private set; }
//
//        public GraphData graph
//        {
//            get
//            {
//                return m_Graph;
//            }
//            set
//            {
//                if (m_Graph != null)
//                    m_Graph.owner = null;
//                m_Graph = value;
////                if (m_Graph != null)
////                    m_Graph.owner = this;
//            }
//        }
//
//        public bool isDirty
//        {
//            get { return m_IsDirty; }
//            set { m_IsDirty = value; }
//        }
//
//        public void RegisterCompleteObjectUndo(string actionName)
//        {
//            Undo.RegisterCompleteObjectUndo(this, actionName);
//            m_SerializedVersion++;
//            version++;
//            m_IsDirty = true;
//        }
//
//        public void OnBeforeSerialize()
//        {
//            if (graph != null)
//            {
//                m_SerializedGraph.JSONnodeData = jsonStore.Serialize(m_Graph, Formatting.None);
//                m_IsSubGraph = graph.isSubGraph;
//                m_AssetGuid = graph.assetGuid;
//            }
//        }
//
//        public void OnAfterDeserialize()
//        {
//        }
//
//        public bool wasUndoRedoPerformed => version != m_SerializedVersion;
//
//        public void HandleUndoRedo()
//        {
//            Debug.Assert(wasUndoRedoPerformed);
//            var deserializedGraph = DeserializeGraph();
//            m_Graph.ReplaceWith(deserializedGraph);
//        }
//
//        GraphData DeserializeGraph()
//        {
//            jsonStore = JsonStore.Deserialize(m_SerializedGraph.JSONnodeData);
//            var deserializedGraph = jsonStore.First<GraphData>();
//            deserializedGraph.isSubGraph = m_IsSubGraph;
//            deserializedGraph.assetGuid = m_AssetGuid;
//            version = m_SerializedVersion;
//            m_SerializedGraph = default;
//            return deserializedGraph;
//        }
//
//        public void Validate()
//        {
//            if (graph != null)
//            {
//                graph.OnEnable();
//                graph.ValidateGraph();
//            }
//        }
//
//        void OnEnable()
//        {
//            if (m_SerializedGraph.JSONnodeData != null)
//            {
//                graph = DeserializeGraph();
//            }
//
//            Validate();
//        }
//    }
//}
