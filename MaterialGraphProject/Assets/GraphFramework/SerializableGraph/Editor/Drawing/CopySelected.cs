using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    [Serializable]
    internal class CopyPasteGraph : ISerializationCallbackReceiver
    {
        [NonSerialized]
        private HashSet<IEdge> m_Edges = new HashSet<IEdge>();

        [NonSerialized]
        private HashSet<INode> m_Nodes = new HashSet<INode>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableEdges = new List<SerializationHelper.JSONSerializedElement>();

        public virtual void AddNode(INode node)
        {
            m_Nodes.Add(node);
        }

        public void AddEdge(IEdge edge)
        {
            m_Edges.Add(edge);
        }

        public IEnumerable<T> GetNodes<T>() where T : INode
        {
            return m_Nodes.OfType<T>();
        }

        public IEnumerable<IEdge> edges
        {
            get { return m_Edges; }
        }

        public virtual void OnBeforeSerialize()
        {
            m_SerializableNodes = SerializationHelper.Serialize<INode>(m_Nodes);
            m_SerializableEdges = SerializationHelper.Serialize<IEdge>(m_Edges);
        }

        public virtual void OnAfterDeserialize()
        {
            var nodes = SerializationHelper.Deserialize<INode>(m_SerializableNodes);
            m_Nodes.Clear();
            foreach (var node in nodes)
                m_Nodes.Add(node);
            m_SerializableNodes = null;

            var edges = SerializationHelper.Deserialize<IEdge>(m_SerializableEdges);
            m_Edges.Clear();
            foreach (var edge in edges)
                m_Edges.Add(edge);
            m_SerializableEdges = null;
        }
    }

    internal class CopySelected : IManipulate
    {
        public delegate void CopyElements(List<CanvasElement> elements);

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.ValidateCommand += Validate;
            element.ExecuteCommand += CopyPaste;
        }

        private bool Validate(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            if (e.commandName != "Copy" && e.commandName != "Paste" && e.commandName != "Duplicate")
                return false;

            e.Use();
            return true;
        }

        private bool CopyPaste(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            if (e.commandName != "Copy" && e.commandName != "Paste" && e.commandName != "Duplicate")
                return false;

            if (e.commandName == "Copy" || e.commandName == "Duplicate")
                DoCopy(parent);

            if (e.commandName == "Paste" || e.commandName == "Duplicate")
                DoPaste(parent);

            e.Use();
            return true;
        }

        private static void DoCopy(Canvas2D parent)
        {
            EditorGUIUtility.systemCopyBuffer = SerializeSelectedElements(parent);
        }

        public static string SerializeSelectedElements(Canvas2D parent)
        {
            var selectedElements = parent.selection;

            // build a graph to serialize (will just contain the
            // nodes and edges we are interested in.
            var graph = new CopyPasteGraph();
            foreach (var thing in selectedElements)
            {
                var dNode = thing as DrawableNode;
                if (dNode != null)
                {
                    graph.AddNode(dNode.m_Node);
                    foreach (var edge in NodeUtils.GetAllEdges(dNode.m_Node))
                        graph.AddEdge(edge);
                }

                var dEdge = thing as DrawableEdge<NodeAnchor>;
                if (dEdge != null)
                {
                    graph.AddEdge(dEdge.m_Edge);
                }
            }
            // serialize then break references
            var serialized = JsonUtility.ToJson(graph, true);
            return serialized;
        }

        public static CopyPasteGraph DeserializeSelectedElements(string toDeserialize)
        {
            try
            {
                return JsonUtility.FromJson<CopyPasteGraph>(toDeserialize);
            }
            catch
            {
                // ignored. just means copy buffer was not a graph :(
                return null;
            }
        }

        private static void DoPaste(Canvas2D parent)
        {
            var copyText = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(copyText))
                return;

            var pastedGraph = DeserializeSelectedElements(copyText);
            if (pastedGraph == null)
                return;

            if (parent.dataSource == null)
                return;

            var dataSource = parent.dataSource as GraphDataSource;
            if (dataSource == null)
                return;

            var asset = dataSource.graphAsset;
            if (asset == null)
                return;

            var graph = asset.graph;
            if (graph == null)
                return;

            var addedNodes = new List<INode>();

            var nodeGuidMap = new Dictionary<Guid, Guid>();
            foreach (var node in pastedGraph.GetNodes<INode>())
            {
                var oldGuid = node.guid;
                var newGuid = node.RewriteGuid();
                nodeGuidMap[oldGuid] = newGuid;

                var drawState = node.drawState;
                var position = drawState.position;
                position.x += 30;
                position.y += 30;
                drawState.position = position;
                node.drawState = drawState;
                graph.AddNode(node);
                addedNodes.Add(node);
            }

            // only connect edges within pasted elements, discard
            // external edges.
            var addedEdges = new List<IEdge>();

            foreach (var edge in pastedGraph.edges)
            {
                var outputSlot = edge.outputSlot;
                var inputSlot = edge.inputSlot;

                Guid remappedOutputNodeGuid;
                Guid remappedInputNodeGuid;
                if (nodeGuidMap.TryGetValue(outputSlot.nodeGuid, out remappedOutputNodeGuid)
                    && nodeGuidMap.TryGetValue(inputSlot.nodeGuid, out remappedInputNodeGuid))
                {
                    var outputSlotRef = new SlotReference(remappedOutputNodeGuid, outputSlot.slotId);
                    var inputSlotRef = new SlotReference(remappedInputNodeGuid, inputSlot.slotId);
                    addedEdges.Add(graph.Connect(outputSlotRef, inputSlotRef));
                }
            }

            graph.ValidateGraph();
            parent.ReloadData();
            parent.Invalidate();

            parent.selection.Clear();
            foreach (var element in parent.elements)
            {
                var drawableNode = element as DrawableNode;
                if (drawableNode != null && addedNodes.Any(x => x == drawableNode.m_Node))
                {
                    drawableNode.selected = true;
                    parent.selection.Add(drawableNode);
                    continue;
                }

                var drawableEdge = element as DrawableEdge<NodeAnchor>;
                if (drawableEdge != null && addedEdges.Any(x => x == drawableEdge.m_Edge))
                {
                    drawableEdge.selected = true;
                    parent.selection.Add(drawableEdge);
                }
            }

            parent.Repaint();
        }
    }
}
