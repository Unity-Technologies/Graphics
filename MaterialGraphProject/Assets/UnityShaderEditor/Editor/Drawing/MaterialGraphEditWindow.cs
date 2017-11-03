using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using Object = UnityEngine.Object;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class MaterialGraphEditWindow : EditorWindow
    {
        [SerializeField]
        Object m_Selected;

        [SerializeField]
        SerializableGraphObject m_GraphObject;

        GraphEditorView m_GraphEditorView;

        GraphEditorView graphEditorView
        {
            get { return m_GraphEditorView; }
            set
            {
                if (m_GraphEditorView != null)
                {
                    rootVisualContainer.Remove(m_GraphEditorView);
                    m_GraphEditorView.Dispose();
                }
                m_GraphEditorView = value;
                if (m_GraphEditorView != null)
                {
                    m_GraphEditorView.onUpdateAssetClick += UpdateAsset;
                    m_GraphEditorView.onConvertToSubgraphClick += ToSubGraph;
                    m_GraphEditorView.onShowInProjectClick += PingAsset;
                    rootVisualContainer.Add(graphEditorView);
                    rootVisualContainer.parent.clippingOptions = VisualElement.ClippingOptions.ClipContents;
                }
            }
        }

        SerializableGraphObject graphObject
        {
            get { return m_GraphObject; }
            set
            {
                if (m_GraphObject != null)
                    DestroyImmediate(m_GraphObject);
                m_GraphObject = value;
            }
        }

        public Object selected
        {
            get { return m_Selected; }
            private set { m_Selected = value; }
        }

        void Update()
        {
            if (graphObject == null)
                return;
            var materialGraph = graphObject.graph as AbstractMaterialGraph;
            if (materialGraph == null)
                return;
            if (graphEditorView == null)
                graphEditorView = new GraphEditorView(materialGraph, selected) { persistenceKey = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selected)) };

            graphEditorView.previewManager.HandleGraphChanges();
            graphEditorView.previewManager.RenderPreviews();
            graphEditorView.HandleGraphChanges();
            graphEditorView.inspectorView.HandleGraphChanges();
            graphObject.graph.ClearChanges();
        }

        void OnDisable()
        {
            graphEditorView = null;
        }

        void OnDestroy()
        {
            if (EditorUtility.DisplayDialog("Shader Graph Might Have Been Modified", "Do you want to save the changes you made in the shader graph?", "Save", "Don't Save"))
            {
                UpdateAsset();
            }
            Undo.ClearUndo(graphObject);
            DestroyImmediate(graphObject);
            graphEditorView = null;
        }

        public void PingAsset()
        {
            if (selected != null)
                EditorGUIUtility.PingObject(selected);
        }

        public void UpdateAsset()
        {
            if (selected != null && graphObject != null)
            {
                var path = AssetDatabase.GetAssetPath(selected);
                if (string.IsNullOrEmpty(path) || graphObject == null)
                {
                    return;
                }

                if (m_GraphObject.graph.GetType() == typeof(ShaderGraph.MaterialGraph))
                    UpdateShaderGraphOnDisk(path);

                if (m_GraphObject.graph.GetType() == typeof(LayeredShaderGraph))
                    UpdateShaderGraphOnDisk(path);

                if (m_GraphObject.graph.GetType() == typeof(SubGraph))
                    UpdateAbstractSubgraphOnDisk<SubGraph>(path);

                if (m_GraphObject.graph.GetType() == typeof(MasterRemapGraph))
                    UpdateAbstractSubgraphOnDisk<MasterRemapGraph>(path);
            }
        }

        public void ToSubGraph()
        {
            var path = EditorUtility.SaveFilePanelInProject("Save subgraph", "New SubGraph", "ShaderSubGraph", "");
            path = path.Replace(Application.dataPath, "Assets");
            if (path.Length == 0)
                return;

            var graphView = graphEditorView.graphView;

            var copyPasteGraph = new CopyPasteGraph(
                graphView.selection.OfType<MaterialNodeView>().Where(x => !(x.node is PropertyNode)).Select(x => x.node as INode),
                graphView.selection.OfType<Edge>().Select(x => x.userData as IEdge));

            var deserialized = CopyPasteGraph.FromJson(JsonUtility.ToJson(copyPasteGraph, false));
            if (deserialized == null)
                return;

            var graph = new SubGraph();
            graph.AddNode(new SubGraphOutputNode());

            var nodeGuidMap = new Dictionary<Guid, Guid>();
            foreach (var node in deserialized.GetNodes<INode>())
            {
                var oldGuid = node.guid;
                var newGuid = node.RewriteGuid();
                nodeGuidMap[oldGuid] = newGuid;
                graph.AddNode(node);
            }

            // remap outputs to the subgraph
            var onlyInputInternallyConnected = new List<IEdge>();
            var onlyOutputInternallyConnected = new List<IEdge>();
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
                    onlyOutputInternallyConnected.Add(edge);
                }
                else if (inputRemapExists)
                {
                    onlyInputInternallyConnected.Add(edge);
                }
            }

            var uniqueInputEdges = onlyOutputInternallyConnected.GroupBy(
                edge => edge.outputSlot,
                edge => edge,
                (key, edges) => new { slotRef = key, edges = edges.ToList() });
            foreach (var group in uniqueInputEdges)
            {
                var sr = group.slotRef;
                var fromNode = graphObject.graph.GetNodeFromGuid(sr.nodeGuid);
                var fromSlot = fromNode.FindOutputSlot<MaterialSlot>(sr.slotId);

                switch (fromSlot.concreteValueType)
                {
                    case ConcreteSlotValueType.SamplerState:
                        break;
                    case ConcreteSlotValueType.Matrix4:
                        break;
                    case ConcreteSlotValueType.Matrix3:
                        break;
                    case ConcreteSlotValueType.Matrix2:
                        break;
                    case ConcreteSlotValueType.Texture2D:
                        break;
                    case ConcreteSlotValueType.Vector4:
                        break;
                    case ConcreteSlotValueType.Vector3:
                        break;
                    case ConcreteSlotValueType.Vector2:
                        break;
                    case ConcreteSlotValueType.Vector1:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var uniqueOutputEdges = onlyInputInternallyConnected.GroupBy(
                edge => edge.inputSlot,
                edge => edge,
                (key, edges) => new { slot = key, edges = edges.ToList() });

            var outputsNeedingConnection = new List<KeyValuePair<IEdge, IEdge>>();
            foreach (var group in uniqueOutputEdges)
            {
                var outputNode = graph.outputNode;
                var slotId = outputNode.AddSlot();

                var inputSlotRef = new SlotReference(outputNode.guid, slotId);

                foreach (var edge in group.edges)
                {
                    var newEdge = graph.Connect(new SlotReference(nodeGuidMap[edge.outputSlot.nodeGuid], edge.outputSlot.slotId), inputSlotRef);
                    outputsNeedingConnection.Add(new KeyValuePair<IEdge, IEdge>(edge, newEdge));
                }
            }

            File.WriteAllText(path, EditorJsonUtility.ToJson(graph));
            AssetDatabase.ImportAsset(path);

            var subGraph = AssetDatabase.LoadAssetAtPath(path, typeof(MaterialSubGraphAsset)) as MaterialSubGraphAsset;
            if (subGraph == null)
                return;

            var subGraphNode = new SubGraphNode();
            graphObject.graph.AddNode(subGraphNode);
            subGraphNode.subGraphAsset = subGraph;

            /*  foreach (var edgeMap in inputsNeedingConnection)
              {
                  graphObject.graph.Connect(edgeMap.Key.outputSlot, new SlotReference(subGraphNode.guid, edgeMap.Value.outputSlot.slotId));
              }*/

            foreach (var edgeMap in outputsNeedingConnection)
            {
                graphObject.graph.Connect(new SlotReference(subGraphNode.guid, edgeMap.Value.inputSlot.slotId), edgeMap.Key.inputSlot);
            }

            graphObject.graph.RemoveElements(
                graphView.selection.OfType<MaterialNodeView>().Select(x => x.node as INode),
                Enumerable.Empty<IEdge>());
            graphObject.graph.ValidateGraph();
        }

        void UpdateAbstractSubgraphOnDisk<T>(string path) where T : AbstractSubGraph
        {
            var graph = graphObject.graph as T;
            if (graph == null)
                return;

            File.WriteAllText(path, EditorJsonUtility.ToJson(graph, true));
            AssetDatabase.ImportAsset(path);
        }

        void UpdateShaderGraphOnDisk(string path)
        {
            var graph = graphObject.graph as IShaderGraph;
            if (graph == null)
                return;

            List<PropertyCollector.TextureInfo> configuredTextures;
            graph.GetShader(Path.GetFileNameWithoutExtension(path), GenerationMode.ForReals, out configuredTextures);

            var shaderImporter = AssetImporter.GetAtPath(path) as ShaderImporter;
            if (shaderImporter == null)
                return;

            var textureNames = new List<string>();
            var textures = new List<Texture>();
            foreach (var textureInfo in configuredTextures.Where(x => x.modifiable))
            {
                var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
                if (texture == null)
                    continue;
                textureNames.Add(textureInfo.name);
                textures.Add(texture);
            }
            shaderImporter.SetDefaultTextures(textureNames.ToArray(), textures.ToArray());

            textureNames.Clear();
            textures.Clear();
            foreach (var textureInfo in configuredTextures.Where(x => !x.modifiable))
            {
                var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
                if (texture == null)
                    continue;
                textureNames.Add(textureInfo.name);
                textures.Add(texture);
            }
            shaderImporter.SetNonModifiableTextures(textureNames.ToArray(), textures.ToArray());
            File.WriteAllText(path, EditorJsonUtility.ToJson(graph, true));
            shaderImporter.SaveAndReimport();
            AssetDatabase.ImportAsset(path);
        }

        public void ChangeSelection(Object newSelection, Type graphType)
        {
            if (!EditorUtility.IsPersistent(newSelection))
                return;

            if (selected == newSelection)
                return;

            selected = newSelection;

            var path = AssetDatabase.GetAssetPath(newSelection);
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            graphObject = CreateInstance<SerializableGraphObject>();
            graphObject.hideFlags = HideFlags.HideAndDontSave;
            graphObject.graph = JsonUtility.FromJson(textGraph, graphType) as IGraph;
            graphObject.graph.OnEnable();
            graphObject.graph.ValidateGraph();

            graphEditorView = new GraphEditorView(m_GraphObject.graph as AbstractMaterialGraph, selected) { persistenceKey = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selected)) };
            graphEditorView.RegisterCallback<PostLayoutEvent>(OnPostLayout);
            titleContent = new GUIContent(selected.name);

            Repaint();
        }

        void OnPostLayout(PostLayoutEvent evt)
        {
            graphEditorView.UnregisterCallback<PostLayoutEvent>(OnPostLayout);
            graphEditorView.graphView.FrameAll();
        }
    }
}
