using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.Experimental.UIElements;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.Graphing.Drawing
{
    public abstract class AbstractGraphEditWindow : EditorWindow, ISerializationCallbackReceiver
    {
        public abstract IGraphAsset inMemoryAsset { get; set; }
        public abstract Object selected { get; set; }

		public static bool allowAlwaysRepaint = true;

        private bool shouldRepaint
        {
            get
            {
				return allowAlwaysRepaint && inMemoryAsset != null && inMemoryAsset.shouldRepaint;
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
            m_GraphEditorDrawer = new GraphEditorDrawer(CreateGraphView());
            rootVisualContainer.AddChild(m_GraphEditorDrawer);
            var source = CreateDataSource();
            source.Initialize(inMemoryAsset, this);
            m_GraphEditorDrawer.presenter = source;
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

		public void PingAsset()
		{
			if (selected != null)
				EditorGUIUtility.PingObject(selected);
		}

        public void UpdateAsset()
        {
            if (selected != null && inMemoryAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(selected);
                if (string.IsNullOrEmpty(path) || inMemoryAsset == null)
                {
                    return;
                }

                var masterNode = ((MaterialGraphAsset)inMemoryAsset).materialGraph.masterNode;
                if (masterNode == null)
                    return;

                List<PropertyGenerator.TextureInfo> configuredTextures;
                masterNode.GetFullShader(GenerationMode.ForReals, out configuredTextures);

                var shaderImporter = AssetImporter.GetAtPath(path) as ShaderImporter;
                if (shaderImporter == null)
                    return;

                var textureNames = new List<string>();
                var textures = new List<Texture>();
                foreach (var textureInfo in configuredTextures.Where(
                    x => x.modifiable == TexturePropertyChunk.ModifiableState.Modifiable))
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
                foreach (var textureInfo in configuredTextures.Where(
                    x => x.modifiable == TexturePropertyChunk.ModifiableState.NonModifiable))
                {
                    var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
                    if (texture == null)
                        continue;
                    textureNames.Add(textureInfo.name);
                    textures.Add(texture);
                }
                shaderImporter.SetNonModifiableTextures(textureNames.ToArray(), textures.ToArray());
                File.WriteAllText(path, EditorJsonUtility.ToJson(inMemoryAsset.graph));
                shaderImporter.SaveAndReimport();
                AssetDatabase.ImportAsset(path);

            }
        }

        public virtual void ToggleRequiresTime()
		{
			allowAlwaysRepaint = !allowAlwaysRepaint;
		}

		public void ChangeSelction(Object newSelection)
		{
			if (!EditorUtility.IsPersistent (newSelection))
				return;

			if (selected == newSelection)
				return;

			if (selected != null)
            {
				if (EditorUtility.DisplayDialog ("Save Old Graph?", "Save Old Graph?", "yes!", "no")) {
					UpdateAsset ();
				}
			}

			selected = newSelection;

		    var mGraph = CreateInstance<MaterialGraphAsset>();
		    var path = AssetDatabase.GetAssetPath(newSelection);
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
		    mGraph.materialGraph = JsonUtility.FromJson<UnityEngine.MaterialGraph.MaterialGraph>(textGraph);

		    inMemoryAsset = mGraph;
            var graph = inMemoryAsset.graph;
			graph.OnEnable ();
			graph.ValidateGraph ();

			var source = CreateDataSource ();
			source.Initialize (inMemoryAsset, this) ;
			m_GraphEditorDrawer.presenter = source;
            Repaint();
        }

		public void OnBeforeSerialize()
		{
			//m_ToLoad = m_Selected as ScriptableObject;
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
