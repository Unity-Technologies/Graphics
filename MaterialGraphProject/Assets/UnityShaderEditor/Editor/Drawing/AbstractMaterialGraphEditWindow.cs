using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing
{
    public interface IMaterialGraphEditWindow
    {
        void PingAsset();

        void UpdateAsset();

        void Repaint();

        void ToggleRequiresTime();
        void ToSubGraph();

        void Show();
        void Focus();
        Object selected { get; set; }
        void ChangeSelection(Object newSelection);
    }

    public abstract class HelperMaterialGraphEditWindow : EditorWindow, IMaterialGraphEditWindow
    {
        public abstract AbstractMaterialGraph GetMaterialGraph();
        public abstract void PingAsset();
        public abstract void UpdateAsset();
        public abstract void ToggleRequiresTime();
        public abstract void ToSubGraph();
        public abstract Object selected { get; set; }
        public abstract void ChangeSelection(Object newSelection);
    }

    public class MaterialGraphEditWindow : AbstractMaterialGraphEditWindow<UnityEngine.MaterialGraph.MaterialGraph>
    {
        public override AbstractMaterialGraph GetMaterialGraph()
        {
            return inMemoryAsset;
        }
    }

    public class SubGraphEditWindow : AbstractMaterialGraphEditWindow<SubGraph>
    {
        public override AbstractMaterialGraph GetMaterialGraph()
        {
            return inMemoryAsset;
        }
    }

    public abstract class AbstractMaterialGraphEditWindow<TGraphType> : HelperMaterialGraphEditWindow where TGraphType : AbstractMaterialGraph
    {
        public static bool allowAlwaysRepaint = true;

        [SerializeField]
        Object m_Selected;

        [SerializeField]
        TGraphType m_InMemoryAsset;

        GraphEditorView m_GraphEditorView;

        protected TGraphType inMemoryAsset
        {
            get { return m_InMemoryAsset; }
            set { m_InMemoryAsset = value; }
        }

        public override Object selected
        {
            get { return m_Selected; }
            set { m_Selected = value; }
        }

        void Update()
        {
            if (m_GraphEditorView != null)
            {
                if (m_GraphEditorView.presenter == null)
                    CreatePresenter();
                m_GraphEditorView.presenter.graphPresenter.UpdateTimeDependentNodes();
            }
        }

        void OnEnable()
        {
            m_GraphEditorView = new GraphEditorView();
            rootVisualContainer.Add(m_GraphEditorView);
        }

        void OnDisable()
        {
            rootVisualContainer.Clear();
        }

        void OnDestroy()
        {
            if (EditorUtility.DisplayDialog("Shader Graph Might Have Been Modified", "Do you want to save the changes you made in the shader graph?", "Save", "Don't Save"))
            {
                UpdateAsset();
            }
        }

        void OnGUI()
        {
            var presenter = m_GraphEditorView.presenter;
            var e = Event.current;

            if (e.type == EventType.ValidateCommand && (
                    e.commandName == "Copy" && presenter.graphPresenter.canCopy
                    || e.commandName == "Paste" && presenter.graphPresenter.canPaste
                    || e.commandName == "Duplicate" && presenter.graphPresenter.canDuplicate
                    || e.commandName == "Cut" && presenter.graphPresenter.canCut
                    || (e.commandName == "Delete" || e.commandName == "SoftDelete") && presenter.graphPresenter.canDelete))
            {
                e.Use();
            }

            if (e.type == EventType.ExecuteCommand)
            {
                if (e.commandName == "Copy")
                    presenter.graphPresenter.Copy();
                if (e.commandName == "Paste")
                    presenter.graphPresenter.Paste();
                if (e.commandName == "Duplicate")
                    presenter.graphPresenter.Duplicate();
                if (e.commandName == "Cut")
                    presenter.graphPresenter.Cut();
                if (e.commandName == "Delete" || e.commandName == "SoftDelete")
                    presenter.graphPresenter.Delete();
            }

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.A)
                    m_GraphEditorView.graphView.FrameAll();
                if (e.keyCode == KeyCode.F)
                    m_GraphEditorView.graphView.FrameSelection();
                if (e.keyCode == KeyCode.O)
                    m_GraphEditorView.graphView.FrameOrigin();
                if (e.keyCode == KeyCode.Tab)
                    m_GraphEditorView.graphView.FrameNext();
                if (e.keyCode == KeyCode.Tab && e.modifiers == EventModifiers.Shift)
                    m_GraphEditorView.graphView.FramePrev();
            }
        }

        public override void PingAsset()
        {
            if (selected != null)
                EditorGUIUtility.PingObject(selected);
        }

        public override void UpdateAsset()
        {
            if (selected != null && inMemoryAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(selected);
                if (string.IsNullOrEmpty(path) || inMemoryAsset == null)
                {
                    return;
                }

                if (typeof(TGraphType) == typeof(UnityEngine.MaterialGraph.MaterialGraph))
                    UpdateShaderGraphOnDisk(path);

                if (typeof(TGraphType) == typeof(SubGraph))
                    UpdateShaderSubGraphOnDisk(path);
            }
        }

        public override void ToSubGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save subgraph", "New SubGraph", "ShaderSubGraph", "");
            path = path.Replace(Application.dataPath, "Assets");
            if (path.Length == 0)
                return;

            var graphPresenter = m_GraphEditorView.presenter.graphPresenter;
            var selected = graphPresenter.elements.Where(e => e.selected).ToArray();
            var deserialized = MaterialGraphPresenter.DeserializeCopyBuffer(JsonUtility.ToJson(MaterialGraphPresenter.CreateCopyPasteGraph(selected)));

            if (deserialized == null)
                return;

            var graph = new SubGraph();
            graph.AddNode(new SubGraphInputNode());
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

            File.WriteAllText(path, EditorJsonUtility.ToJson(graph));
            AssetDatabase.ImportAsset(path);

            var subGraph = AssetDatabase.LoadAssetAtPath(path, typeof(MaterialSubGraphAsset)) as MaterialSubGraphAsset;
            if (subGraph == null)
                return;

            var subGraphNode = new SubGraphNode();
            graphPresenter.AddNode(subGraphNode);
            subGraphNode.subGraphAsset = subGraph;

            foreach (var edgeMap in inputsNeedingConnection)
            {
                graphPresenter.graph.Connect(edgeMap.Key.outputSlot, new SlotReference(subGraphNode.guid, edgeMap.Value.outputSlot.slotId));
            }

            foreach (var edgeMap in outputsNeedingConnection)
            {
                graphPresenter.graph.Connect(new SlotReference(subGraphNode.guid, edgeMap.Value.inputSlot.slotId), edgeMap.Key.inputSlot);
            }

            var toDelete = graphPresenter.elements.Where(e => e.selected).OfType<MaterialNodePresenter>();
            graphPresenter.RemoveElements(toDelete, new List<GraphEdgePresenter>());
        }

        private void UpdateShaderSubGraphOnDisk(string path)
        {
            var graph = inMemoryAsset as SubGraph;
            if (graph == null)
                return;

            File.WriteAllText(path, EditorJsonUtility.ToJson(inMemoryAsset, true));
            AssetDatabase.ImportAsset(path);
        }

        private void UpdateShaderGraphOnDisk(string path)
        {
            var graph = inMemoryAsset as UnityEngine.MaterialGraph.MaterialGraph;
            if (graph == null)
                return;

            List<PropertyCollector.TextureInfo> configuredTextures;
            graph.GetFullShader(GenerationMode.ForReals, Path.GetFileNameWithoutExtension(path), out configuredTextures);

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
            File.WriteAllText(path, EditorJsonUtility.ToJson(inMemoryAsset, true));
            shaderImporter.SaveAndReimport();
            AssetDatabase.ImportAsset(path);
        }

        public override void ToggleRequiresTime()
        {
            allowAlwaysRepaint = !allowAlwaysRepaint;
        }

        public override void ChangeSelection(Object newSelection)
        {
            if (!EditorUtility.IsPersistent(newSelection))
                return;

            if (selected == newSelection)
                return;

            selected = newSelection;

            var path = AssetDatabase.GetAssetPath(newSelection);
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            inMemoryAsset = JsonUtility.FromJson<TGraphType>(textGraph);
            inMemoryAsset.OnEnable();
            inMemoryAsset.ValidateGraph();

            CreatePresenter();
            titleContent = new GUIContent(selected.name);

            Repaint();
        }

        void CreatePresenter()
        {
            var presenter = CreateInstance<GraphEditorPresenter>();
            presenter.Initialize(inMemoryAsset, this, selected.name);
            m_GraphEditorView.presenter = presenter;
            m_GraphEditorView.RegisterCallback<PostLayoutEvent>(OnPostLayout);
        }

        void OnPostLayout(PostLayoutEvent evt)
        {
            m_GraphEditorView.UnregisterCallback<PostLayoutEvent>(OnPostLayout);
            m_GraphEditorView.graphView.FrameAll();
        }
    }
}
