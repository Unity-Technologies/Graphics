using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public abstract class AbstractGraphEditWindow<T> : EditorWindow, ISerializationCallbackReceiver where T : class, IGraphAsset
    {
        public RenderTexture rt;

        [NonSerialized]
        private T m_LastSelection;

        [SerializeField]
        private ScriptableObject m_LastSelectedGraphSerialized;

        private bool shouldRepaint
        {
            get
            {
                return m_LastSelection != null && m_LastSelection.shouldRepaint;
            }
        }

        protected GraphView graphView
        {
            get { return m_GraphView; }
        }

        private GraphView m_GraphView;

        private TitleBarDrawer m_TitleBarDrawer;

        public virtual AbstractGraphDataSource CreateDataSource()
        {
            return CreateInstance<SerializedGraphDataSource>();
        }

        public virtual GraphView CreateGraphView()
        {
            return new SerializableGraphView();
        }

        void OnEnable()
        {
            m_GraphView = CreateGraphView();
            m_GraphView.name = "Graph View";
            var source = CreateDataSource();
            source.Initialize(m_LastSelection);

            var titleBarData = CreateInstance<TitleBarDrawData>();
            titleBarData.title = m_LastSelection != null ? m_LastSelection.GetScriptableObject().name : "";
            m_TitleBarDrawer = new TitleBarDrawer(titleBarData);

            m_GraphView.dataSource = source;
            m_GraphView.StretchToParentSize();

            rootVisualContainer.AddChild(m_TitleBarDrawer);
            rootVisualContainer.AddChild(m_GraphView);
        }

        void OnDisable()
        {
            rootVisualContainer.ClearChildren();
        }

        void Update()
        {
            if (shouldRepaint)
                Repaint();
        }

        void OnSelectionChange()
        {
            if (Selection.activeObject == null || !EditorUtility.IsPersistent(Selection.activeObject))
                return;

            if (Selection.activeObject is ScriptableObject)
            {
                var selection = Selection.activeObject as T;
                if (selection != m_LastSelection)
                {
                    var graph = selection.graph;
                    graph.OnEnable();
                    graph.ValidateGraph();
                    m_LastSelection = selection;


                    var source = CreateDataSource();
                    source.Initialize(m_LastSelection);
                    m_GraphView.dataSource = source;

                    var titleBarData = CreateInstance<TitleBarDrawData>();
                    titleBarData.title = m_LastSelection != null ? m_LastSelection.GetScriptableObject().name : "";
                    m_TitleBarDrawer.dataProvider = titleBarData;

                    m_GraphView.StretchToParentSize();
                    Repaint();
                }
            }
        }

        /*


        private void ConvertSelectionToSubGraph()
        {
            if (m_Canvas.dataSource == null)
                return;

            var dataSource = m_Canvas.dataSource as GraphDataSource;
            if (dataSource == null)
                return;

            var asset = dataSource.graphAsset;
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
            dataSource.DeleteElements(toDelete);

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
        }

        private void Rebuild()
        {
            if (m_Canvas == null || m_LastSelection == null)
                return;

            m_DataSource.graphAsset = m_LastSelection;
            m_Canvas.ReloadData();
        }*/

        /*     void OnGUI()
             {
                 m_HostWindow = this;
                 if (m_Canvas == null)
                 {
                     InitializeCanvas();
                 }

                 if (m_LastSelection == null || m_LastSelection.graph == null)
                 {
                     GUILayout.Label("No Graph selected");
                     return;
                 }

                 m_Canvas.OnGUI(this, new Rect(0, 0, position.width - 250, position.height));

                 if (GUI.Button(new Rect(position.width - 250, 0, 250, 50), "Convert to Sub-Graph"))
                     ConvertSelectionToSubGraph();

                 if (GUI.Button(new Rect(position.width - 250, 70, 250, 50), "Export"))
                     Export(false);


                 if (GUI.Button(new Rect(position.width - 250, 140, 250, 50), "Export - quick"))
                     Export(true);


                 EditorGUI.ObjectField(new Rect(position.width - 250, 210, 250, 50), rt, typeof(RenderTexture), false);
             }*/


        /*public void RenderOptions(MaterialGraph graph)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos, EditorStyles.textArea, GUILayout.width(250), GUILayout.ExpandHeight(true));
            graph.materialOptions.DoGUI();
            EditorGUILayout.Separator();

            m_NodeExpanded = MaterialGraphStyles.Header("Selected", m_NodeExpanded);
            if (m_NodeExpanded)
                DrawableMaterialNode.OnGUI(m_Canvas.selection);

            GUILayout.EndScrollView();
            if (GUILayout.Button("Export"))
                m_DataSource.Export(false);

            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }*/

        public void OnBeforeSerialize()
        {
            var o = m_LastSelection as ScriptableObject;
            if (o != null)
                m_LastSelectedGraphSerialized = o;
        }

        public void OnAfterDeserialize()
        {
            if (m_LastSelectedGraphSerialized != null)
                m_LastSelection = m_LastSelectedGraphSerialized as T;

            m_LastSelectedGraphSerialized = null;
        }
    }
}
