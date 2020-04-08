using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;

using UnityEditor.UIElements;
using Edge = UnityEditor.Experimental.GraphView.Edge;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using UnityEditor.VersionControl;

namespace UnityEditor.ShaderGraph.Drawing
{
    class MaterialGraphEditWindow : EditorWindow
    {
        // For conversion to Sub Graph: keys for remembering the user's desired path
        const string k_PrevSubGraphPathKey = "SHADER_GRAPH_CONVERT_TO_SUB_GRAPH_PATH";
        const string k_PrevSubGraphPathDefaultValue = "?"; // Special character that NTFS does not allow, so that no directory could match it.

        [SerializeField]
        string m_Selected;

        [SerializeField]
        GraphObject m_GraphObject;

        [NonSerialized]
        HashSet<string> m_ChangedFileDependencies = new HashSet<string>();

        ColorSpace m_ColorSpace;
        RenderPipelineAsset m_RenderPipelineAsset;

        [NonSerialized]
        bool m_FrameAllAfterLayout;
        [NonSerialized]
        bool m_HasError;
        [NonSerialized]
        bool m_ProTheme;
        [NonSerialized]
        bool m_Deleted;

        MessageManager m_MessageManager;
        MessageManager messageManager
        {
            get { return m_MessageManager ?? (m_MessageManager = new MessageManager()); }
        }

        GraphEditorView m_GraphEditorView;
        GraphEditorView graphEditorView
        {
            get { return m_GraphEditorView; }
            set
            {
                if (m_GraphEditorView != null)
                {
                    m_GraphEditorView.RemoveFromHierarchy();
                    m_GraphEditorView.Dispose();
                }

                m_GraphEditorView = value;

                if (m_GraphEditorView != null)
                {
                    m_GraphEditorView.saveRequested += UpdateAsset;
                    m_GraphEditorView.saveAsRequested += SaveAs;
                    m_GraphEditorView.convertToSubgraphRequested += ToSubGraph;
                    m_GraphEditorView.showInProjectRequested += PingAsset;
                    m_GraphEditorView.isCheckedOut += IsGraphAssetCheckedOut;
                    m_GraphEditorView.checkOut += CheckoutAsset;
                    m_GraphEditorView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    m_FrameAllAfterLayout = true;
                    this.rootVisualElement.Add(graphEditorView);
                }
            }
        }

        GraphObject graphObject
        {
            get { return m_GraphObject; }
            set
            {
                if (m_GraphObject != null)
                    DestroyImmediate(m_GraphObject);
                m_GraphObject = value;
            }
        }

        public string selectedGuid
        {
            get { return m_Selected; }
            private set
            {
                m_Selected = value;
            }
        }

        public string assetName
        {
            get { return titleContent.text; }
            set
            {
                titleContent.text = value;
                graphEditorView.assetName = value;
            }
        }

        void DisplayChangedOnDiskDialog()
        {
            if (EditorUtility.DisplayDialog("Graph has changed on disk, do you want to reload?", AssetDatabase.GUIDToAssetPath(selectedGuid), "Reload", "Don't Reload"))
            {
                graphObject = null;
            }
        }

        void DisplayDeletedFromDiskDialog()
        {
            bool shouldClose = true; // Close unless if the same file was replaced

            if (EditorUtility.DisplayDialog("\"" + assetName + "\" Graph Asset Missing", AssetDatabase.GUIDToAssetPath(selectedGuid)
                    + " has been deleted or moved outside of Unity.\n\nWould you like to save your Graph Asset?", "Save As", "Close Window"))
            {
                shouldClose = !SaveAsImplementation();
            }

            if (shouldClose)
                Close();
            else
                m_Deleted = false; // Was restored
        }

        void Update()
        {
            if (m_HasError)
                return;

            if (focusedWindow == this && m_Deleted)
                DisplayDeletedFromDiskDialog();

            if (PlayerSettings.colorSpace != m_ColorSpace)
            {
                graphEditorView = null;
                m_ColorSpace = PlayerSettings.colorSpace;
            }

            if (GraphicsSettings.renderPipelineAsset != m_RenderPipelineAsset)
            {
                graphEditorView = null;
                m_RenderPipelineAsset = GraphicsSettings.renderPipelineAsset;
            }

            if (EditorGUIUtility.isProSkin != m_ProTheme)
            {
                if (graphObject != null && graphObject.graph != null)
                {
                    Texture2D icon = GetThemeIcon(graphObject.graph);

                    // This is adding the icon at the front of the tab
                    titleContent = EditorGUIUtility.TrTextContentWithIcon(assetName, icon);
                    m_ProTheme = EditorGUIUtility.isProSkin;
                }
            }

            if (m_PromptChangedOnDisk)
            {
                m_PromptChangedOnDisk = false;
                DisplayChangedOnDiskDialog();
            }

            try
            {
                if (graphObject == null && selectedGuid != null)
                {
                    var guid = selectedGuid;
                    selectedGuid = null;
                    Initialize(guid);
                }

                if (graphObject == null)
                {
                    Close();
                    return;
                }

                var materialGraph = graphObject.graph as GraphData;
                if (materialGraph == null)
                    return;

                if (graphEditorView == null)
                {
                    messageManager.ClearAll();
                    materialGraph.messageManager = messageManager;
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(selectedGuid));
                    graphEditorView = new GraphEditorView(this, materialGraph, messageManager)
                    {
                        viewDataKey = selectedGuid,
                        assetName = asset.name.Split('/').Last()
                    };
                    m_ColorSpace = PlayerSettings.colorSpace;
                    m_RenderPipelineAsset = GraphicsSettings.renderPipelineAsset;
                    graphObject.Validate();
                }

                if (m_ChangedFileDependencies.Count > 0 && graphObject != null && graphObject.graph != null)
                {
                    var subGraphNodes = graphObject.graph.GetNodes<SubGraphNode>();
                    foreach (var subGraphNode in subGraphNodes)
                    {
                        subGraphNode.Reload(m_ChangedFileDependencies);
                    }
                    if(subGraphNodes.Count() > 0)
                    {
                        // Keywords always need to be updated to test against variant limit
                        // No Keywords may indicate removal and this may have now made the Graph valid again
                        // Need to validate Graph to clear errors in this case
                        materialGraph.OnKeywordChanged();
                    }
                    foreach (var customFunctionNode in graphObject.graph.GetNodes<CustomFunctionNode>())
                    {
                        customFunctionNode.Reload(m_ChangedFileDependencies);
                    }

                    m_ChangedFileDependencies.Clear();
                }

                var wasUndoRedoPerformed = graphObject.wasUndoRedoPerformed;

                if (wasUndoRedoPerformed)
                {
                    graphEditorView.HandleGraphChanges();
                    graphObject.graph.ClearChanges();
                    graphObject.HandleUndoRedo();
                }

                if (graphObject.isDirty || wasUndoRedoPerformed)
                {
                    UpdateTitle();
                    graphObject.isDirty = false;
                }

                graphEditorView.HandleGraphChanges();
                graphObject.graph.ClearChanges();
            }
            catch (Exception e)
            {
                m_HasError = true;
                m_GraphEditorView = null;
                graphObject = null;
                Debug.LogException(e);
                throw;
            }
        }

        public void ReloadSubGraphsOnNextUpdate(List<string> changedFiles)
        {
            foreach (var changedFile in changedFiles)
            {
                m_ChangedFileDependencies.Add(changedFile);
            }
        }

        void OnEnable()
        {
            this.SetAntiAliasing(4);
        }

        void OnDisable()
        {
            graphEditorView = null;
            messageManager.ClearAll();
        }

        bool IsDirty()
        {
            if (m_Deleted)
                return false; // Not dirty; it's gone.

            var currentJson = EditorJsonUtility.ToJson(graphObject.graph, true);
            var fileJson = File.ReadAllText(AssetDatabase.GUIDToAssetPath(selectedGuid));
            return !string.Equals(currentJson, fileJson, StringComparison.Ordinal);
        }

        [SerializeField]
        bool m_PromptChangedOnDisk;

        public void CheckForChanges()
        {
            var isDirty = IsDirty();
            if (isDirty)
            {
                m_PromptChangedOnDisk = true;
            }
            UpdateTitle(isDirty);
        }

        public void AssetWasDeleted()
        {
            m_Deleted = true;
        }

        void UpdateTitle()
        {
            UpdateTitle(IsDirty());
        }

        void UpdateTitle(bool isDirty)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(selectedGuid));
            titleContent.text = asset.name.Split('/').Last() + (isDirty ? "*" : "");
        }

        void OnDestroy()
        {
            if (graphObject != null)
            {
                string nameOfFile = AssetDatabase.GUIDToAssetPath(selectedGuid);
                if (IsDirty() && EditorUtility.DisplayDialog("Shader Graph Has Been Modified", "Do you want to save the changes you made in the Shader Graph?\n" + nameOfFile + "\n\nYour changes will be lost if you don't save them.", "Save", "Don't Save"))
                    UpdateAsset();
                Undo.ClearUndo(graphObject);
                graphObject = null;
            }

            graphEditorView = null;
        }

        public void PingAsset()
        {
            if (selectedGuid != null)
            {
                var path = AssetDatabase.GUIDToAssetPath(selectedGuid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                EditorGUIUtility.PingObject(asset);
            }
        }

        public bool IsGraphAssetCheckedOut()
        {
            if (selectedGuid != null)
            {
                var path = AssetDatabase.GUIDToAssetPath(selectedGuid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (!AssetDatabase.IsOpenForEdit(asset, StatusQueryOptions.UseCachedIfPossible))
                    return false;

                return true;
            }

            return false;
        }

        public void CheckoutAsset()
        {
            if (selectedGuid != null)
            {
                var path = AssetDatabase.GUIDToAssetPath(selectedGuid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                Task task = Provider.Checkout(asset, CheckoutMode.Both);
                task.Wait();
            }
        }

        public void UpdateAsset()
        {
            if (selectedGuid != null && graphObject != null)
            {
                var path = AssetDatabase.GUIDToAssetPath(selectedGuid);
                if (string.IsNullOrEmpty(path) || graphObject == null)
                    return;

                ShaderGraphAnalytics.SendShaderGraphEvent(selectedGuid, graphObject.graph);

                var oldShader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (oldShader != null)
                    ShaderUtil.ClearShaderMessages(oldShader);

                UpdateShaderGraphOnDisk(path);

                if (GraphData.onSaveGraph != null)
                {
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                    if (shader != null)
                    {
                        GraphData.onSaveGraph(shader, (graphObject.graph.outputNode as AbstractMaterialNode).saveContext);
                    }
                }
            }

            UpdateTitle();
        }

        public void SaveAs()
        {
            SaveAsImplementation();
        }

        // Returns true if the same file as replaced, false if a new file was created or an error occured
        bool SaveAsImplementation()
        {
            if (selectedGuid != null && graphObject != null)
            {
                var pathAndFile = AssetDatabase.GUIDToAssetPath(selectedGuid);
                if (string.IsNullOrEmpty(pathAndFile) || graphObject == null)
                    return false;

                // The asset's name needs to be removed from the path, otherwise SaveFilePanel assumes it's a folder
                string path = Path.GetDirectoryName(pathAndFile);

                var extension = graphObject.graph.isSubGraph ? ShaderSubGraphImporter.Extension : ShaderGraphImporter.Extension;
                var newPath = EditorUtility.SaveFilePanelInProject("Save Graph As...", Path.GetFileNameWithoutExtension(pathAndFile), extension, "", path);
                newPath = newPath.Replace(Application.dataPath, "Assets");

                if (newPath != path)
                {
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        var success = FileUtilities.WriteShaderGraphToDisk(newPath, graphObject.graph);
                        AssetDatabase.ImportAsset(newPath);
                        if (success)
                        {
                            ShaderGraphImporterEditor.ShowGraphEditWindow(newPath);
                            // This is for updating material dependencies so we exclude subgraphs here.
                            if (GraphData.onSaveGraph != null && extension != ShaderSubGraphImporter.Extension)
                            {
                                var shader = AssetDatabase.LoadAssetAtPath<Shader>(newPath);
                                // Retrieve graph context, note that if we're here the output node will always be a master node
                                GraphData.onSaveGraph(shader, (graphObject.graph.outputNode as AbstractMaterialNode).saveContext);
                            }
                        }
                    }

                    graphObject.isDirty = false;
                    return false;
                }
                else
                {
                    UpdateAsset();
                    graphObject.isDirty = false;
                    return true;
                }
            }

            return false;
        }

        public void ToSubGraph()
        {
            var graphView = graphEditorView.graphView;

            string path;
            string sessionStateResult = SessionState.GetString(k_PrevSubGraphPathKey, k_PrevSubGraphPathDefaultValue);
            string pathToOriginSG = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(selectedGuid));

            if (!sessionStateResult.Equals(k_PrevSubGraphPathDefaultValue))
            {
                path = sessionStateResult;
            }
            else
            {
                path = pathToOriginSG;
            }

            path = EditorUtility.SaveFilePanelInProject("Save Sub Graph", "New Shader Sub Graph", ShaderSubGraphImporter.Extension, "", path);
            path = path.Replace(Application.dataPath, "Assets");
            if (path.Length == 0)
                return;

            graphObject.RegisterCompleteObjectUndo("Convert To Subgraph");

            var nodes = graphView.selection.OfType<IShaderNodeView>().Where(x => !(x.node is PropertyNode || x.node is SubGraphOutputNode)).Select(x => x.node).Where(x => x.allowedInSubGraph).ToArray();
            var bounds = Rect.MinMaxRect(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity);
            foreach (var node in nodes)
            {
                var center = node.drawState.position.center;
                bounds = Rect.MinMaxRect(
                        Mathf.Min(bounds.xMin, center.x),
                        Mathf.Min(bounds.yMin, center.y),
                        Mathf.Max(bounds.xMax, center.x),
                        Mathf.Max(bounds.yMax, center.y));
            }
            var middle = bounds.center;
            bounds.center = Vector2.zero;

            // Collect graph inputs
            var graphInputs = graphView.selection.OfType<BlackboardField>().Select(x => x.userData as ShaderInput);

            // Collect the property nodes and get the corresponding properties
            var propertyNodeGuids = graphView.selection.OfType<IShaderNodeView>().Where(x => (x.node is PropertyNode)).Select(x => ((PropertyNode)x.node).propertyGuid);
            var metaProperties = graphView.graph.properties.Where(x => propertyNodeGuids.Contains(x.guid));

            // Collect the keyword nodes and get the corresponding keywords
            var keywordNodeGuids = graphView.selection.OfType<IShaderNodeView>().Where(x => (x.node is KeywordNode)).Select(x => ((KeywordNode)x.node).keywordGuid);
            var metaKeywords = graphView.graph.keywords.Where(x => keywordNodeGuids.Contains(x.guid));

            var copyPasteGraph = new CopyPasteGraph(
                    graphView.graph.assetGuid,
                    graphView.selection.OfType<ShaderGroup>().Select(x => x.userData),
                    graphView.selection.OfType<IShaderNodeView>().Where(x => !(x.node is PropertyNode || x.node is SubGraphOutputNode)).Select(x => x.node).Where(x => x.allowedInSubGraph).ToArray(),
                    graphView.selection.OfType<Edge>().Select(x => x.userData as IEdge),
                    graphInputs,
                    metaProperties,
                    metaKeywords,
                    graphView.selection.OfType<StickyNote>().Select(x => x.userData));

            var deserialized = CopyPasteGraph.FromJson(JsonUtility.ToJson(copyPasteGraph, false));
            if (deserialized == null)
                return;

            var subGraph = new GraphData { isSubGraph = true };
            subGraph.path = "Sub Graphs";
            var subGraphOutputNode = new SubGraphOutputNode();
            {
                var drawState = subGraphOutputNode.drawState;
                drawState.position = new Rect(new Vector2(bounds.xMax + 200f, 0f), drawState.position.size);
                subGraphOutputNode.drawState = drawState;
            }
            subGraph.AddNode(subGraphOutputNode);

            // Always copy deserialized keyword inputs
            foreach (ShaderKeyword keyword in deserialized.metaKeywords)
            {
                ShaderInput copiedInput = keyword.Copy();
                subGraph.SanitizeGraphInputName(copiedInput);
                subGraph.SanitizeGraphInputReferenceName(copiedInput, keyword.overrideReferenceName);
                subGraph.AddGraphInput(copiedInput);

                // Update the keyword nodes that depends on the copied keyword
                var dependentKeywordNodes = deserialized.GetNodes<KeywordNode>().Where(x => x.keywordGuid == keyword.guid);
                foreach (var node in dependentKeywordNodes)
                {
                    node.owner = graphView.graph;
                    node.keywordGuid = copiedInput.guid;
                }
            }

            var groupGuidMap = new Dictionary<Guid, Guid>();
            foreach (GroupData groupData in deserialized.groups)
            {
                var oldGuid = groupData.guid;
                var newGuid = groupData.RewriteGuid();
                groupGuidMap[oldGuid] = newGuid;
                subGraph.CreateGroup(groupData);
            }

            List<Guid> groupGuids = new List<Guid>();
            var nodeGuidMap = new Dictionary<Guid, Guid>();
            foreach (var node in deserialized.GetNodes<AbstractMaterialNode>())
            {
                var oldGuid = node.guid;
                var newGuid = node.RewriteGuid();
                nodeGuidMap[oldGuid] = newGuid;
                var drawState = node.drawState;
                drawState.position = new Rect(drawState.position.position - middle, drawState.position.size);
                node.drawState = drawState;

                if (!groupGuids.Contains(node.groupGuid))
                {
                    groupGuids.Add(node.groupGuid);
                }

                // Checking if the group guid is also being copied.
                // If not then nullify that guid
                if (node.groupGuid != Guid.Empty)
                {
                    node.groupGuid = !groupGuidMap.ContainsKey(node.groupGuid) ? Guid.Empty : groupGuidMap[node.groupGuid];
                }

                subGraph.AddNode(node);
            }

            foreach (var note in deserialized.stickyNotes)
            {
                if (!groupGuids.Contains(note.groupGuid))
                {
                    groupGuids.Add(note.groupGuid);
                }

                if (note.groupGuid != Guid.Empty)
                {
                    note.groupGuid = !groupGuidMap.ContainsKey(note.groupGuid) ? Guid.Empty : groupGuidMap[note.groupGuid];
                }

                note.RewriteGuid();
                subGraph.AddStickyNote(note);
            }

            // figure out what needs remapping
            var externalOutputSlots = new List<IEdge>();
            var externalInputSlots = new List<IEdge>();
            foreach (var edge in deserialized.edges)
            {
                var outputSlot = edge.outputSlot;
                var inputSlot = edge.inputSlot;

                Guid remappedOutputNodeGuid;
                Guid remappedInputNodeGuid;
                var outputSlotExistsInSubgraph = nodeGuidMap.TryGetValue(outputSlot.nodeGuid, out remappedOutputNodeGuid);
                var inputSlotExistsInSubgraph = nodeGuidMap.TryGetValue(inputSlot.nodeGuid, out remappedInputNodeGuid);

                // pasting nice internal links!
                if (outputSlotExistsInSubgraph && inputSlotExistsInSubgraph)
                {
                    var outputSlotRef = new SlotReference(remappedOutputNodeGuid, outputSlot.slotId);
                    var inputSlotRef = new SlotReference(remappedInputNodeGuid, inputSlot.slotId);
                    subGraph.Connect(outputSlotRef, inputSlotRef);
                }
                // one edge needs to go to outside world
                else if (outputSlotExistsInSubgraph)
                {
                    externalInputSlots.Add(edge);
                }
                else if (inputSlotExistsInSubgraph)
                {
                    externalOutputSlots.Add(edge);
                }
            }

            // Find the unique edges coming INTO the graph
            var uniqueIncomingEdges = externalOutputSlots.GroupBy(
                    edge => edge.outputSlot,
                    edge => edge,
                    (key, edges) => new { slotRef = key, edges = edges.ToList() });

            var externalInputNeedingConnection = new List<KeyValuePair<IEdge, AbstractShaderProperty>>();

            var amountOfProps = uniqueIncomingEdges.Count();
            const int height = 40;
            const int subtractHeight = 20;
            var propPos = new Vector2(0, -((amountOfProps / 2) + height) - subtractHeight);

            foreach (var group in uniqueIncomingEdges)
            {
                var sr = group.slotRef;
                var fromNode = graphObject.graph.GetNodeFromGuid(sr.nodeGuid);
                var fromSlot = fromNode.FindOutputSlot<MaterialSlot>(sr.slotId);

                AbstractShaderProperty prop;
                switch (fromSlot.concreteValueType)
                {
                    case ConcreteSlotValueType.Texture2D:
                        prop = new Texture2DShaderProperty();
                        break;
                    case ConcreteSlotValueType.Texture2DArray:
                        prop = new Texture2DArrayShaderProperty();
                        break;
                    case ConcreteSlotValueType.Texture3D:
                        prop = new Texture3DShaderProperty();
                        break;
                    case ConcreteSlotValueType.Cubemap:
                        prop = new CubemapShaderProperty();
                        break;
                    case ConcreteSlotValueType.Vector4:
                        prop = new Vector4ShaderProperty();
                        break;
                    case ConcreteSlotValueType.Vector3:
                        prop = new Vector3ShaderProperty();
                        break;
                    case ConcreteSlotValueType.Vector2:
                        prop = new Vector2ShaderProperty();
                        break;
                    case ConcreteSlotValueType.Vector1:
                        prop = new Vector1ShaderProperty();
                        break;
                    case ConcreteSlotValueType.Boolean:
                        prop = new BooleanShaderProperty();
                        break;
                    case ConcreteSlotValueType.Matrix2:
                        prop = new Matrix2ShaderProperty();
                        break;
                    case ConcreteSlotValueType.Matrix3:
                        prop = new Matrix3ShaderProperty();
                        break;
                    case ConcreteSlotValueType.Matrix4:
                        prop = new Matrix4ShaderProperty();
                        break;
                    case ConcreteSlotValueType.SamplerState:
                        prop = new SamplerStateShaderProperty();
                        break;
                    case ConcreteSlotValueType.Gradient:
                        prop = new GradientShaderProperty();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (prop != null)
                {
                    var materialGraph = (GraphData)graphObject.graph;
                    var fromPropertyNode = fromNode as PropertyNode;
                    var fromProperty = fromPropertyNode != null ? materialGraph.properties.FirstOrDefault(p => p.guid == fromPropertyNode.propertyGuid) : null;
                    prop.displayName = fromProperty != null ? fromProperty.displayName : fromSlot.concreteValueType.ToString();
                    prop.displayName = GraphUtil.SanitizeName(subGraph.addedInputs.Select(p => p.displayName), "{0} ({1})", prop.displayName);

                    subGraph.AddGraphInput(prop);
                    var propNode = new PropertyNode();
                    {
                        var drawState = propNode.drawState;
                        drawState.position = new Rect(new Vector2(bounds.xMin - 300f, 0f) + propPos, drawState.position.size);
                        propPos += new Vector2(0, height);
                        propNode.drawState = drawState;
                    }
                    subGraph.AddNode(propNode);
                    propNode.propertyGuid = prop.guid;

                    foreach (var edge in group.edges)
                    {
                        subGraph.Connect(
                            new SlotReference(propNode.guid, PropertyNode.OutputSlotId),
                            new SlotReference(nodeGuidMap[edge.inputSlot.nodeGuid], edge.inputSlot.slotId));
                        externalInputNeedingConnection.Add(new KeyValuePair<IEdge, AbstractShaderProperty>(edge, prop));
                    }
                }
            }

            var uniqueOutgoingEdges = externalInputSlots.GroupBy(
                    edge => edge.outputSlot,
                    edge => edge,
                    (key, edges) => new { slot = key, edges = edges.ToList() });

            var externalOutputsNeedingConnection = new List<KeyValuePair<IEdge, IEdge>>();
            foreach (var group in uniqueOutgoingEdges)
            {
                var outputNode = subGraph.outputNode as SubGraphOutputNode;

                AbstractMaterialNode node = graphView.graph.GetNodeFromGuid(group.edges[0].outputSlot.nodeGuid);
                MaterialSlot slot = node.FindSlot<MaterialSlot>(group.edges[0].outputSlot.slotId);
                var slotId = outputNode.AddSlot(slot.concreteValueType);

                var inputSlotRef = new SlotReference(outputNode.guid, slotId);

                foreach (var edge in group.edges)
                {
                    var newEdge = subGraph.Connect(new SlotReference(nodeGuidMap[edge.outputSlot.nodeGuid], edge.outputSlot.slotId), inputSlotRef);
                    externalOutputsNeedingConnection.Add(new KeyValuePair<IEdge, IEdge>(edge, newEdge));
                }
            }

            if (FileUtilities.WriteShaderGraphToDisk(path, subGraph))
                AssetDatabase.ImportAsset(path);

            // Store path for next time
            if (!pathToOriginSG.Equals(Path.GetDirectoryName(path)))
            {
                SessionState.SetString(k_PrevSubGraphPathKey, Path.GetDirectoryName(path));
            }
            else
            {
                // Or continue to make it so that next time it will open up in the converted-from SG's directory
                SessionState.EraseString(k_PrevSubGraphPathKey);
            }

            var loadedSubGraph = AssetDatabase.LoadAssetAtPath(path, typeof(SubGraphAsset)) as SubGraphAsset;
            if (loadedSubGraph == null)
                return;

            var subGraphNode = new SubGraphNode();
            var ds = subGraphNode.drawState;
            ds.position = new Rect(middle - new Vector2(100f, 150f), Vector2.zero);
            subGraphNode.drawState = ds;

            // Add the subgraph into the group if the nodes was all in the same group group
            if (groupGuids.Count == 1)
            {
                subGraphNode.groupGuid = groupGuids[0];
            }

            graphObject.graph.AddNode(subGraphNode);
            subGraphNode.asset = loadedSubGraph;

            foreach (var edgeMap in externalInputNeedingConnection)
            {
                graphObject.graph.Connect(edgeMap.Key.outputSlot, new SlotReference(subGraphNode.guid, edgeMap.Value.guid.GetHashCode()));
            }

            foreach (var edgeMap in externalOutputsNeedingConnection)
            {
                graphObject.graph.Connect(new SlotReference(subGraphNode.guid, edgeMap.Value.inputSlot.slotId), edgeMap.Key.inputSlot);
            }

            graphObject.graph.RemoveElements(
                graphView.selection.OfType<IShaderNodeView>().Select(x => x.node).Where(x => x.allowedInSubGraph).ToArray(),
                new IEdge[] {},
                new GroupData[] {},
                graphView.selection.OfType<StickyNote>().Select(x => x.userData).ToArray());
            graphObject.graph.ValidateGraph();
        }

        void UpdateShaderGraphOnDisk(string path)
        {
            if(FileUtilities.WriteShaderGraphToDisk(path, graphObject.graph))
                AssetDatabase.ImportAsset(path);
        }

        public void Initialize(string assetGuid)
        {
            try
            {
                m_ColorSpace = PlayerSettings.colorSpace;
                m_RenderPipelineAsset = GraphicsSettings.renderPipelineAsset;

                var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(assetGuid));
                if (asset == null)
                    return;

                if (!EditorUtility.IsPersistent(asset))
                    return;

                if (selectedGuid == assetGuid)
                    return;

                var path = AssetDatabase.GetAssetPath(asset);
                var extension = Path.GetExtension(path);
                if (extension == null)
                    return;
                // Path.GetExtension returns the extension prefixed with ".", so we remove it. We force lower case such that
                // the comparison will be case-insensitive.
                extension = extension.Substring(1).ToLowerInvariant();
                bool isSubGraph;
                switch (extension)
                {
                    case ShaderGraphImporter.Extension:
                        isSubGraph = false;
                        break;
                    case ShaderSubGraphImporter.Extension:
                        isSubGraph = true;
                        break;
                    default:
                        return;
                }

                selectedGuid = assetGuid;

                var textGraph = File.ReadAllText(path, Encoding.UTF8);
                graphObject = CreateInstance<GraphObject>();
                graphObject.hideFlags = HideFlags.HideAndDontSave;
                graphObject.graph = JsonUtility.FromJson<GraphData>(textGraph);
                graphObject.graph.assetGuid = assetGuid;
                graphObject.graph.isSubGraph = isSubGraph;
                graphObject.graph.messageManager = messageManager;
                graphObject.graph.OnEnable();
                graphObject.graph.ValidateGraph();

                graphEditorView = new GraphEditorView(this, m_GraphObject.graph, messageManager)
                {
                    viewDataKey = selectedGuid,
                    assetName = asset.name.Split('/').Last()
                };

                Texture2D icon = GetThemeIcon(graphObject.graph);

                // This is adding the icon at the front of the tab
                titleContent = EditorGUIUtility.TrTextContentWithIcon(selectedGuid, icon);
                UpdateTitle();

                Repaint();
            }
            catch (Exception)
            {
                m_HasError = true;
                m_GraphEditorView = null;
                graphObject = null;
                throw;
            }
        }

        Texture2D GetThemeIcon(GraphData graphdata)
        {
            string theme = EditorGUIUtility.isProSkin ? "_dark" : "_light";
            Texture2D icon = Resources.Load<Texture2D>("Icons/sg_graph_icon_gray"+theme+"@16");
            if (graphdata.isSubGraph)
            {
                icon = Resources.Load<Texture2D>("Icons/sg_subgraph_icon_gray"+theme+"@16");
            }

            return icon;
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // this callback is only so we can run post-layout behaviors after the graph loads for the first time
            // we immediately unregister it so it doesn't get called again
            graphEditorView.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (m_FrameAllAfterLayout)
                graphEditorView.graphView.FrameAll();
            m_FrameAllAfterLayout = false;
        }
    }
}
