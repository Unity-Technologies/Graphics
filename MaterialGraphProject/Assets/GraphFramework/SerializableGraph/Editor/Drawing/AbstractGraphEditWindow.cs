using System;
using System.IO;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Graphing.Drawing
{
    // TODO JOCE Derive from GraphViewEditorWindow
    public abstract class AbstractGraphEditWindow : EditorWindow, ISerializationCallbackReceiver
    {
		[NonSerialized]
		private IGraphAsset m_Selected;

		[NonSerialized]
		private IGraphAsset m_InMemoryAsset;

		[SerializeField]
		private ScriptableObject m_ToLoad;

        private bool shouldRepaint
        {
            get
            {
				return m_InMemoryAsset != null && m_InMemoryAsset.shouldRepaint;
            }
        }

        protected GraphView graphView
        {
            get { return m_GraphEditorDrawer.graphView; }
        }

        private GraphEditorDrawer m_GraphEditorDrawer;

        public virtual AbstractGraphPresenter CreateDataSource()
        {
            return CreateInstance<SerializedGraphPresenter>();
        }

        public virtual GraphView CreateGraphView()
        {
            return new SerializableGraphView();
        }

        void OnEnable()
        {
            var source = CreateDataSource();
			source.Initialize(m_InMemoryAsset, this);

            m_GraphEditorDrawer = new GraphEditorDrawer(CreateGraphView(), source);
            rootVisualContainer.AddChild(m_GraphEditorDrawer);
        }

        void OnDisable()
        {
            rootVisualContainer.ClearChildren();
        }

        void Update()
        {
			if (m_ToLoad) 
			{
				ChangeSelction (m_ToLoad as IGraphAsset);
				m_ToLoad = null;
			}

            if (shouldRepaint)
                Repaint();
        }

        private bool focused { get; set; }
        private void Focus(TimerState timerState)
        {
            m_GraphEditorDrawer.graphView.FrameAll();
            focused = true;
        }

		public void PingAsset()
		{
			if (m_Selected != null)
				EditorGUIUtility.PingObject(m_Selected.GetScriptableObject());
		}

		public void UpdateAsset()
		{
			if (m_Selected != null && m_Selected is IGraphAsset)
			{
				var path = AssetDatabase.GetAssetPath (m_Selected.GetScriptableObject());

				if (!string.IsNullOrEmpty(path) && m_InMemoryAsset != null) 
				{
					File.WriteAllText (path, EditorJsonUtility.ToJson (m_InMemoryAsset.graph as object));
					AssetDatabase.Refresh ();

					var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject> (path) as IGraphAsset;
					if (asset != null)
						ChangeSelction (asset, false);
				}
			}
		}

		public void ChangeSelction(IGraphAsset newSelection, bool refocus = true)
		{
			if (!(newSelection is ScriptableObject))
				return;

			var newGraph = (ScriptableObject)newSelection;
			if (!EditorUtility.IsPersistent (newGraph))
				return;

			if (m_Selected == newSelection)
				return;

			if (m_Selected != null) {
				if (EditorUtility.DisplayDialog ("Save Old Graph?", "Save Old Graph?", "yes!", "no")) {
					UpdateAsset ();
				}
			}

			m_Selected = newSelection;

			m_InMemoryAsset = UnityEngine.Object.Instantiate (newGraph) as IGraphAsset;
               
			var graph = m_InMemoryAsset.graph;
			graph.OnEnable ();
			graph.ValidateGraph ();

			var source = CreateDataSource ();
			source.Initialize (m_InMemoryAsset, this) ;
			m_GraphEditorDrawer.presenter = source;
			//m_GraphView.StretchToParentSize();
			Repaint ();
			if (refocus) 
			{
				focused = false;
				m_GraphEditorDrawer.graphView.Schedule (Focus).StartingIn (1).Until (() => focused);
			}
		}

		public void OnBeforeSerialize()
		{
			m_ToLoad = m_Selected as ScriptableObject;
		}

		public void OnAfterDeserialize()
		{}

        /*
        private void ConvertSelectionToSubGraph()
        {
            if (m_Canvas.presenter == null)
                return;

            var presenter = m_Canvas.presenter as GraphDataSource;
            if (presenter == null)
                return;

            var asset = presenter.graphAsset;
            if (asset == null)
                return;

            var targetGraph = asset.graph;
            if (targetGraph == null)
                return;

            if (!m_Canvas.selection.Any())
                return;

            var serialzied = CopySelected.SerializeSelectedElements(m_Canvas);
            var deserialized = CopySelected.DeserializeSelectedElements(serialzied);
            if (deserialized == null)
                return;

            string path = EditorUtility.SaveFilePanelInProject("Save subgraph", "New SubGraph", "ShaderSubGraph", "");
            path = path.Replace(Application.dataPath, "Assets");
            if (path.Length == 0)
                return;

            var graphAsset = CreateInstance<MaterialSubGraphAsset>();
            graphAsset.name = Path.GetFileName(path);
            graphAsset.PostCreate();

            var graph = graphAsset.subGraph;
            if (graphAsset.graph == null)
                return;

            var nodeGuidMap = new Dictionary<Guid, Guid>();
            foreach (var node in deserialized.GetNodes<INode>())
            {
                var oldGuid = node.guid;
                var newGuid = node.RewriteGuid();
                nodeGuidMap[oldGuid] = newGuid;
                graph.AddNode(node);
            }

            // remap outputs to the subgraph
            var inputEdgeNeedsRemap = new List<IEdge>();
            var outputEdgeNeedsRemap = new List<IEdge>();
            foreach (var edge in deserialized.edges)
            {
                var outputSlot = edge.outputSlot;
                var inputSlot = edge.inputSlot;

                Guid remappedOutputNodeGuid;
                Guid remappedInputNodeGuid;
                var outputRemapExists = nodeGuidMap.TryGetValue(outputSlot.nodeGuid, out remappedOutputNodeGuid);
                var inputRemapExists = nodeGuidMap.TryGetValue(inputSlot.nodeGuid, out remappedInputNodeGuid);

                // pasting nice internal links!
                if (outputRemapExists && inputRemapExists)
                {
                    var outputSlotRef = new SlotReference(remappedOutputNodeGuid, outputSlot.slotId);
                    var inputSlotRef = new SlotReference(remappedInputNodeGuid, inputSlot.slotId);
                    graph.Connect(outputSlotRef, inputSlotRef);
                }
                // one edge needs to go to outside world
                else if (outputRemapExists)
                {
                    inputEdgeNeedsRemap.Add(edge);
                }
                else if (inputRemapExists)
                {
                    outputEdgeNeedsRemap.Add(edge);
                }
            }

            // we do a grouping here as the same output can
            // point to multiple inputs
            var uniqueOutputs = outputEdgeNeedsRemap.GroupBy(edge => edge.outputSlot);
            var inputsNeedingConnection = new List<KeyValuePair<IEdge, IEdge>>();
            foreach (var group in uniqueOutputs)
            {
                var inputNode = graph.inputNode;
                var slotId = inputNode.AddSlot();

                var outputSlotRef = new SlotReference(inputNode.guid, slotId);

                foreach (var edge in group)
                {
                    var newEdge = graph.Connect(outputSlotRef, new SlotReference(nodeGuidMap[edge.inputSlot.nodeGuid], edge.inputSlot.slotId));
                    inputsNeedingConnection.Add(new KeyValuePair<IEdge, IEdge>(edge, newEdge));
                }
            }

            var uniqueInputs = inputEdgeNeedsRemap.GroupBy(edge => edge.inputSlot);
            var outputsNeedingConnection = new List<KeyValuePair<IEdge, IEdge>>();
            foreach (var group in uniqueInputs)
            {
                var outputNode = graph.outputNode;
                var slotId = outputNode.AddSlot();

                var inputSlotRef = new SlotReference(outputNode.guid, slotId);

                foreach (var edge in group)
                {
                    var newEdge = graph.Connect(new SlotReference(nodeGuidMap[edge.outputSlot.nodeGuid], edge.outputSlot.slotId), inputSlotRef);
                    outputsNeedingConnection.Add(new KeyValuePair<IEdge, IEdge>(edge, newEdge));
                }
            }
            AssetDatabase.CreateAsset(graphAsset, path);

            var subGraphNode = new SubGraphNode();
            targetGraph.AddNode(subGraphNode);
            subGraphNode.subGraphAsset = graphAsset;

            foreach (var edgeMap in inputsNeedingConnection)
            {
                targetGraph.Connect(edgeMap.Key.outputSlot, new SlotReference(subGraphNode.guid, edgeMap.Value.outputSlot.slotId));
            }
            foreach (var edgeMap in outputsNeedingConnection)
            {
                targetGraph.Connect(new SlotReference(subGraphNode.guid, edgeMap.Value.inputSlot.slotId), edgeMap.Key.inputSlot);
            }

            var toDelete = m_Canvas.selection.Where(x => x is DrawableNode).ToList();
            presenter.DeleteElements(toDelete);

            targetGraph.ValidateGraph();
            m_Canvas.ReloadData();
            m_Canvas.Invalidate();
            m_Canvas.selection.Clear();

            var toSelect = m_Canvas.elements.OfType<DrawableNode>().FirstOrDefault(x => x.m_Node == subGraphNode);
            if (toSelect != null)
            {
                toSelect.selected = true;
                m_Canvas.selection.Add(toSelect);
            }
            m_Canvas.Repaint();
        }*/


    }
}
