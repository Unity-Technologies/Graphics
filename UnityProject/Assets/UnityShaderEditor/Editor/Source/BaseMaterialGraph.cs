using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public abstract class BaseMaterialGraph : ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<Edge> m_Edges = new List<Edge>();
        
        [SerializeField]
        private List<BaseMaterialNode> m_Nodes = new List<BaseMaterialNode>();

        private PreviewRenderUtility m_PreviewUtility;

        private MaterialGraph m_Owner;

        public BaseMaterialGraph(MaterialGraph owner)
        {
            m_Owner = owner;
        }

        public IEnumerable<BaseMaterialNode> nodes { get { return m_Nodes; } } 
        public IEnumerable<Edge> edges { get { return m_Edges; } } 

        public PreviewRenderUtility previewUtility
        {
            get
            {
                if (m_PreviewUtility == null)
                {
                    m_PreviewUtility = new PreviewRenderUtility();
                    // EditorUtility.SetCameraAnimateMaterials(m_PreviewUtility.m_Camera, true);
                }

                return m_PreviewUtility;
            }
        }

        public bool requiresRepaint
        {
            get { return m_Nodes.Any(x => x is IRequiresTime); }
        }

        public MaterialGraph owner
        {
            get { return m_Owner; }
        }

        public void RemoveEdge(Edge e)
        {
            m_Edges.Remove(e);
            RevalidateGraph();
        }

        public void RemoveEdgeNoRevalidate(Edge e)
        {
            m_Edges.Remove(e);
        }

        public void RemoveNode(BaseMaterialNode node)
        {
            if (!node.canDeleteNode)
                return;

            m_Nodes.Remove(node);
            RevalidateGraph();
        }

        public void RemoveNodeNoRevalidate(BaseMaterialNode node)
        {
            if (!node.canDeleteNode)
                return;

            m_Nodes.Remove(node);
        }

        public BaseMaterialNode GetNodeFromGUID(Guid guid)
        {
            return m_Nodes.FirstOrDefault(x => x.guid == guid);
        }

        public IEnumerable<Edge> GetEdges(Slot s)
        {
            return m_Edges.Where(x =>
                (x.outputSlot.nodeGuid == s.owner.guid && x.outputSlot.slotName == s.name)
                || x.inputSlot.nodeGuid == s.owner.guid && x.inputSlot.slotName == s.name);
        }

        public Edge Connect(Slot fromSlot, Slot toSlot)
        {
            Slot outputSlot = null;
            Slot inputSlot = null;

            // output must connect to input
            if (fromSlot.isOutputSlot)
                outputSlot = fromSlot;
            else if (fromSlot.isInputSlot)
                inputSlot = fromSlot;

            if (toSlot.isOutputSlot)
                outputSlot = toSlot;
            else if (toSlot.isInputSlot)
                inputSlot = toSlot;

            if (inputSlot == null || outputSlot == null)
                return null;

            var edges = GetEdges(inputSlot).ToList();
            // remove any inputs that exits before adding
            foreach (var edge in edges)
            {
                Debug.Log("Removing existing edge:" + edge);
                // call base here as we DO NOT want to
                // do expensive shader regeneration
                RemoveEdge(edge);
            }

            var newEdge = new Edge(new SlotReference(outputSlot.owner.guid, outputSlot.name), new SlotReference(inputSlot.owner.guid, inputSlot.name));
            m_Edges.Add(newEdge);

            Debug.Log("Connected edge: " + newEdge);
            RevalidateGraph();
            return newEdge;
        }

        public virtual void RevalidateGraph()
        {
            //First validate edges, remove any
            //orphans. This can happen if a user
            //manually modifies serialized data
            //of if they delete a node in the inspector
            //debug view.
            var allNodeGUIDs = m_Nodes.Select(x => x.guid).ToList();

            foreach (var edge in edges.ToArray())
            {
                if (allNodeGUIDs.Contains(edge.inputSlot.nodeGuid) && allNodeGUIDs.Contains(edge.outputSlot.nodeGuid))
                    continue;

                //orphaned edge
                m_Edges.Remove(edge);
            }

            var bmns = m_Nodes.Where(x => x is BaseMaterialNode).ToList();
            
            foreach (var node in bmns)
                node.ValidateNode();
        }

        public void AddNode(BaseMaterialNode node)
        {
            m_Nodes.Add(node);
            RevalidateGraph();
        }

        private void AddNodeNoValidate(BaseMaterialNode node)
        {
            m_Nodes.Add(node);
        }

        private static string GetTypeSerializableAsString(Type type)
        {
            if (type == null)
                return string.Empty;

            return string.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name);
        }

        private static Type GetTypeFromSerializedString(string type)
        {
            if (string.IsNullOrEmpty(type))
                return null;

            return Type.GetType(type);
        }

        [Serializable]
        struct SerializableNode
        {
            [SerializeField]
            public string typeName;

            [SerializeField]
            public string JSONnodeData;
        }

        [SerializeField]
        List<SerializableNode> m_SerializableNodes = new List<SerializableNode>(); 
        public void OnBeforeSerialize()
        {
            m_SerializableNodes.Clear();

            foreach (var node in nodes)
            {
                if (node == null)
                    continue;

                var typeName = GetTypeSerializableAsString(node.GetType());
                var data = JsonUtility.ToJson(node, true);

                if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(data))
                    continue;

                m_SerializableNodes.Add( new SerializableNode()
                {
                    typeName = typeName,
                    JSONnodeData = data
                });
            }
        }

        public void OnAfterDeserialize()
        {
            m_Nodes.Clear();
            foreach (var serializedNode in m_SerializableNodes) 
            {
                if (string.IsNullOrEmpty(serializedNode.typeName) || string.IsNullOrEmpty(serializedNode.JSONnodeData))
                    continue;

                var type = GetTypeFromSerializedString(serializedNode.typeName);
                if (type == null)
                {
                    Debug.LogWarningFormat("Could not find node of type {0} in loaded assemblies", serializedNode.typeName);
                    continue;
                }

                BaseMaterialNode node;
                try
                {
                    var constructorInfo = type.GetConstructor(new[] { typeof(BaseMaterialGraph) });
                    node = (BaseMaterialNode)constructorInfo.Invoke(new object[] { this });
                }
                catch
                {
                    Debug.LogWarningFormat("Could not construct instance of: {0} as there is no single argument constuctor that takes a BaseMaterialGraph", type);
                    continue;
                }
                JsonUtility.FromJsonOverwrite(serializedNode.JSONnodeData, node);
                AddNodeNoValidate(node);
            }
            RevalidateGraph(); 
        }
    }
}
