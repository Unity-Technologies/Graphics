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
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.UIElements;
using UnityEditor.VersionControl;

using Unity.Profiling;
using UnityEngine.Assertions;

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

        // this stores the contents of the file on disk, as of the last time we saved or loaded it from disk
        [SerializeField]
        string m_LastSerializedFileContents;

        [NonSerialized]
        HashSet<string> m_ChangedFileDependencyGUIDs = new HashSet<string>();

        ColorSpace m_ColorSpace;
        RenderPipelineAsset m_RenderPipelineAsset;

        [NonSerialized]
        bool m_FrameAllAfterLayout;
        [NonSerialized]
        bool m_HasError;
        [NonSerialized]
        bool m_ProTheme;
        [NonSerialized]
        int m_customInterpWarn;
        [NonSerialized]
        int m_customInterpErr;

        [SerializeField]
        bool m_AssetMaybeChangedOnDisk;

        [SerializeField]
        bool m_AssetMaybeDeleted;

        MessageManager m_MessageManager;
        MessageManager messageManager
        {
            get { return m_MessageManager ?? (m_MessageManager = new MessageManager()); }
        }

        GraphEditorView m_GraphEditorView;
        internal GraphEditorView graphEditorView
        {
            get { return m_GraphEditorView; }
            private set
            {
                if (m_GraphEditorView != null)
                {
                    m_GraphEditorView.RemoveFromHierarchy();
                    m_GraphEditorView.Dispose();
                }

                m_GraphEditorView = value;

                if (m_GraphEditorView != null)
                {
                    m_GraphEditorView.saveRequested += () => SaveAsset();
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

        internal GraphObject graphObject
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
        }

        bool AssetFileExists()
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(selectedGuid);
            return File.Exists(assetPath);
        }

        // returns true when the graph has been successfully saved, or the user has indicated they are ok with discarding the local graph
        // returns false when saving has failed
        bool DisplayDeletedFromDiskDialog(bool reopen = true)
        {
            // first double check if we've actually been deleted
            bool saved = false;
            bool okToClose = false;
            string originalAssetPath = AssetDatabase.GUIDToAssetPath(selectedGuid);

            while (true)
            {
                int option = EditorUtility.DisplayDialogComplex(
                    "Graph removed from project",
                    "The file has been deleted or removed from the project folder.\n\n" +
                    originalAssetPath +
                    "\n\nWould you like to save your Graph Asset?",
                    "Save As...", "Cancel", "Discard Graph and Close Window");

                if (option == 0)
                {
                    string savedPath = SaveAsImplementation(false);
                    if (savedPath != null)
                    {
                        saved = true;

                        // either close or reopen the local window editor
                        graphObject = null;
                        selectedGuid = (reopen ? AssetDatabase.AssetPathToGUID(savedPath) : null);

                        break;
                    }
                }
                else if (option == 2)
                {
                    okToClose = true;
                    graphObject = null;
                    selectedGuid = null;
                    break;
                }
                else if (option == 1)
                {
                    // continue in deleted state...
                    break;
                }
            }

            return (saved || okToClose);
        }

        void Update()
        {
            if (m_HasError)
                return;

            bool updateTitle = false;

            if (m_AssetMaybeDeleted)
            {
                m_AssetMaybeDeleted = false;
                if (AssetFileExists())
                {
                    // it exists... just to be sure, let's check if it changed
                    m_AssetMaybeChangedOnDisk = true;
                }
                else
                {
                    // it was really deleted, ask the user what to do
                    bool handled = DisplayDeletedFromDiskDialog(true);
                }
                updateTitle = true;
            }

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
                    updateTitle = true; // trigger icon swap
                    m_ProTheme = EditorGUIUtility.isProSkin;
                }
            }

            bool revalidate = false;
            if (m_customInterpWarn != ShaderGraphProjectSettings.instance.customInterpolatorWarningThreshold)
            {
                m_customInterpWarn = ShaderGraphProjectSettings.instance.customInterpolatorWarningThreshold;
                revalidate = true;
            }
            if (m_customInterpErr != ShaderGraphProjectSettings.instance.customInterpolatorErrorThreshold)
            {
                m_customInterpErr = ShaderGraphProjectSettings.instance.customInterpolatorErrorThreshold;
                revalidate = true;
            }
            if (revalidate)
            {
                graphEditorView?.graphView?.graph?.ValidateGraph();
            }

            if (m_AssetMaybeChangedOnDisk)
            {
                m_AssetMaybeChangedOnDisk = false;

                // if we don't have any graph, then it doesn't really matter if the file on disk changed or not
                // as we're going to reload it below anyways
                if (graphObject?.graph != null)
                {
                    // check if it actually did change on disk
                    if (FileOnDiskHasChanged())
                    {
                        // don't worry people about "losing changes" unless there are changes to lose
                        bool graphChanged = GraphHasChangedSinceLastSerialization();

                        if (EditorUtility.DisplayDialog(
                            "Graph has changed on disk",
                            AssetDatabase.GUIDToAssetPath(selectedGuid) + "\n\n" +
                            (graphChanged ? "Do you want to reload it and lose the changes made in the graph?" : "Do you want to reload it?"),
                            graphChanged ? "Discard Changes And Reload" : "Reload",
                            "Don't Reload"))
                        {
                            // clear graph, trigger reload
                            graphObject = null;
                        }
                    }
                }
                updateTitle = true;
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
                    string assetPath = AssetDatabase.GUIDToAssetPath(selectedGuid);
                    string graphName = Path.GetFileNameWithoutExtension(assetPath);

                    graphEditorView = new GraphEditorView(this, materialGraph, messageManager, graphName)
                    {
                        viewDataKey = selectedGuid,
                    };
                    m_ColorSpace = PlayerSettings.colorSpace;
                    m_RenderPipelineAsset = GraphicsSettings.renderPipelineAsset;
                    graphObject.Validate();

                    // update blackboard title for the new graphEditorView
                    updateTitle = true;
                }

                if (m_ChangedFileDependencyGUIDs.Count > 0 && graphObject != null && graphObject.graph != null)
                {
                    bool reloadedSomething = false;
                    var subGraphNodes = graphObject.graph.GetNodes<SubGraphNode>();
                    foreach (var subGraphNode in subGraphNodes)
                    {
                        var reloaded = subGraphNode.Reload(m_ChangedFileDependencyGUIDs);
                        reloadedSomething |= reloaded;
                    }
                    if (subGraphNodes.Count() > 0)
                    {
                        // Keywords always need to be updated to test against variant limit
                        // No Keywords may indicate removal and this may have now made the Graph valid again
                        // Need to validate Graph to clear errors in this case
                        materialGraph.OnKeywordChanged();

                        UpdateDropdownEntries();
                        materialGraph.OnDropdownChanged();
                    }
                    foreach (var customFunctionNode in graphObject.graph.GetNodes<CustomFunctionNode>())
                    {
                        var reloaded = customFunctionNode.Reload(m_ChangedFileDependencyGUIDs);
                        reloadedSomething |= reloaded;
                    }

                    // reloading files may change serialization
                    if (reloadedSomething)
                    {
                        updateTitle = true;

                        // may also need to re-run validation/concretization
                        graphObject.Validate();
                    }

                    m_ChangedFileDependencyGUIDs.Clear();
                }

                var wasUndoRedoPerformed = graphObject.wasUndoRedoPerformed;

                if (wasUndoRedoPerformed)
                {
                    graphEditorView.HandleGraphChanges(true);
                    graphObject.graph.ClearChanges();
                    graphObject.HandleUndoRedo();
                }

                if (graphObject.isDirty || wasUndoRedoPerformed)
                {
                    updateTitle = true;
                    graphObject.isDirty = false;
                    hasUnsavedChanges = false;
                }

                // Called again to handle changes from deserialization in case an undo/redo was performed
                graphEditorView.HandleGraphChanges(wasUndoRedoPerformed);
                graphObject.graph.ClearChanges();

                if (updateTitle)
                    UpdateTitle();
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

        public void ReloadSubGraphsOnNextUpdate(List<string> changedFileGUIDs)
        {
            foreach (var changedFileGUID in changedFileGUIDs)
            {
                m_ChangedFileDependencyGUIDs.Add(changedFileGUID);
            }
        }

        void UpdateDropdownEntries()
        {
            var subGraphNodes = graphObject.graph.GetNodes<SubGraphNode>();
            foreach (var subGraphNode in subGraphNodes)
            {
                var nodeView = graphEditorView.graphView.nodes.ToList().OfType<IShaderNodeView>()
                    .FirstOrDefault(p => p.node != null && p.node == subGraphNode);
                if (nodeView != null)
                {
                    nodeView.UpdateDropdownEntries();
                }
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

        // returns true only when the file on disk doesn't match the graph we last loaded or saved to disk (i.e. someone else changed it)
        internal bool FileOnDiskHasChanged()
        {
            var currentFileJson = ReadAssetFile();
            return !string.Equals(currentFileJson, m_LastSerializedFileContents, StringComparison.Ordinal);
        }

        // returns true only when the graph in this window would serialize different from the last time we loaded or saved it
        internal bool GraphHasChangedSinceLastSerialization()
        {
            Assert.IsTrue(graphObject?.graph != null); // this should be checked by calling code
            var currentGraphJson = MultiJson.Serialize(graphObject.graph);
            return !string.Equals(currentGraphJson, m_LastSerializedFileContents, StringComparison.Ordinal);
        }

        // returns true only when saving the graph in this window would serialize different from the file on disk
        internal bool GraphIsDifferentFromFileOnDisk()
        {
            Assert.IsTrue(graphObject?.graph != null); // this should be checked by calling code
            var currentGraphJson = MultiJson.Serialize(graphObject.graph);
            var currentFileJson = ReadAssetFile();
            return !string.Equals(currentGraphJson, currentFileJson, StringComparison.Ordinal);
        }

        // NOTE: we're using the AssetPostprocessor Asset Import and Deleted callbacks as a proxy for detecting file changes
        // We could probably replace both m_AssetMaybeDeleted and  m_AssetMaybeChangedOnDisk with a combined "need to check the real status of the file" flag
        public void CheckForChanges()
        {
            if (!m_AssetMaybeDeleted && graphObject?.graph != null)
            {
                m_AssetMaybeChangedOnDisk = true;
                UpdateTitle();
            }
        }

        public void AssetWasDeleted()
        {
            m_AssetMaybeDeleted = true;
            UpdateTitle();
        }

        public void UpdateTitle()
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(selectedGuid);
            string shaderName = Path.GetFileNameWithoutExtension(assetPath);

            // update blackboard title (before we add suffixes)
            if (graphEditorView != null)
                graphEditorView.assetName = shaderName;

            // build the window title (with suffixes)
            string title = shaderName;
            if (graphObject?.graph == null)
                title = title + " (nothing loaded)";
            else
            {
                if (GraphHasChangedSinceLastSerialization())
                {
                    hasUnsavedChanges = true;
                    // This is the message EditorWindow will show when prompting to close while dirty
                    saveChangesMessage = GetSaveChangesMessage();
                }
                else
                {
                    hasUnsavedChanges = false;
                    saveChangesMessage = "";
                }
                if (!AssetFileExists())
                    title = title + " (deleted)";
            }

            // get window icon
            bool isSubGraph = graphObject?.graph?.isSubGraph ?? false;
            Texture2D icon;
            {
                string theme = EditorGUIUtility.isProSkin ? "_dark" : "_light";
                if (isSubGraph)
                    icon = Resources.Load<Texture2D>("Icons/sg_subgraph_icon_gray" + theme);
                else
                    icon = Resources.Load<Texture2D>("Icons/sg_graph_icon_gray" + theme);
            }

            titleContent = new GUIContent(title, icon);
        }

        void OnDestroy()
        {
            // Prompting the user if they want to close is mostly handled via the EditorWindow's system (hasUnsavedChanges).
            // There's unfortunately a code path (Reload Window) that doesn't go through this path. The old logic is left
            // here as a fallback to catch this. This has one edge case with "Reload Window" -> "Cancel" which will produce
            // two shader graph windows: one unmodified (that the editor opens) and one modified (that we open below).

            // we are closing the shadergraph window
            MaterialGraphEditWindow newWindow = null;
            if (!PromptSaveIfDirtyOnQuit())
            {
                // user does not want to close the window.
                // we can't stop the close from this code path though..
                // all we can do is open a new window and transfer our data to the new one to avoid losing it
                // newWin = Instantiate<MaterialGraphEditWindow>(this);
                newWindow = EditorWindow.CreateWindow<MaterialGraphEditWindow>(typeof(MaterialGraphEditWindow), typeof(SceneView));
                newWindow.Initialize(this);
            }
            else
            {
                // the window is closing for good.. cleanup undo history for the graph object
                Undo.ClearUndo(graphObject);
            }

            graphObject = null;
            graphEditorView = null;

            // show new window if we have one
            if (newWindow != null)
            {
                newWindow.Show();
                newWindow.Focus();
            }
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

        // returns true if the asset was succesfully saved
        public bool SaveAsset()
        {
            bool saved = false;

            if (selectedGuid != null && graphObject != null)
            {
                var path = AssetDatabase.GUIDToAssetPath(selectedGuid);
                if (string.IsNullOrEmpty(path) || graphObject == null)
                    return false;

                if (GraphUtil.CheckForRecursiveDependencyOnPendingSave(path, graphObject.graph.GetNodes<SubGraphNode>(), "Save"))
                    return false;

                ShaderGraphAnalytics.SendShaderGraphEvent(selectedGuid, graphObject.graph);

                var oldShader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (oldShader != null)
                    ShaderUtil.ClearShaderMessages(oldShader);

                var newFileContents = FileUtilities.WriteShaderGraphToDisk(path, graphObject.graph);
                if (newFileContents != null)
                {
                    saved = true;
                    m_LastSerializedFileContents = newFileContents;
                    AssetDatabase.ImportAsset(path);
                }

                OnSaveGraph(path);
                hasUnsavedChanges = false;
            }

            UpdateTitle();

            return saved;
        }

        void OnSaveGraph(string path)
        {
            if (GraphData.onSaveGraph == null)
                return;

            if (graphObject.graph.isSubGraph)
                return;

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null)
                return;

            foreach (var target in graphObject.graph.activeTargets)
            {
                GraphData.onSaveGraph(shader, target.saveContext);
            }
        }

        public void SaveAs()
        {
            SaveAsImplementation(true);
        }

        // returns the asset path the file was saved to, or NULL if nothing was saved
        string SaveAsImplementation(bool openWhenSaved)
        {
            string savedFilePath = null;

            if (selectedGuid != null && graphObject?.graph != null)
            {
                var oldFilePath = AssetDatabase.GUIDToAssetPath(selectedGuid);
                if (string.IsNullOrEmpty(oldFilePath) || graphObject == null)
                    return null;

                // The asset's name needs to be removed from the path, otherwise SaveFilePanel assumes it's a folder
                string oldDirectory = Path.GetDirectoryName(oldFilePath);

                var extension = graphObject.graph.isSubGraph ? ShaderSubGraphImporter.Extension : ShaderGraphImporter.Extension;
                var newFilePath = EditorUtility.SaveFilePanelInProject("Save Graph As...", Path.GetFileNameWithoutExtension(oldFilePath), extension, "", oldDirectory);
                newFilePath = newFilePath.Replace(Application.dataPath, "Assets");

                if (newFilePath != oldFilePath)
                {
                    if (!string.IsNullOrEmpty(newFilePath))
                    {
                        // If the newPath already exists, we are overwriting an existing file, and could be creating recursions. Let's check.
                        if (GraphUtil.CheckForRecursiveDependencyOnPendingSave(newFilePath, graphObject.graph.GetNodes<SubGraphNode>(), "Save As"))
                            return null;

                        bool success = (FileUtilities.WriteShaderGraphToDisk(newFilePath, graphObject.graph) != null);
                        AssetDatabase.ImportAsset(newFilePath);
                        if (success)
                        {
                            if (openWhenSaved)
                                ShaderGraphImporterEditor.ShowGraphEditWindow(newFilePath);
                            OnSaveGraph(newFilePath);
                            savedFilePath = newFilePath;
                        }
                    }
                }
                else
                {
                    // saving to the current path
                    if (SaveAsset())
                    {
                        graphObject.isDirty = false;
                        savedFilePath = oldFilePath;
                    }
                }
            }
            return savedFilePath;
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

            // Friendly warning that the user is generating a subgraph that would overwrite the one they are currently working on.
            if (AssetDatabase.AssetPathToGUID(path) == selectedGuid)
            {
                if (!EditorUtility.DisplayDialog("Overwrite Current Subgraph", "Do you want to overwrite this Sub Graph that you are currently working on? You cannot undo this operation.", "Yes", "Cancel"))
                {
                    path = "";
                }
            }

            if (path.Length == 0)
                return;

            var nodes = graphView.selection.OfType<IShaderNodeView>().Where(x => !(x.node is PropertyNode || x.node is SubGraphOutputNode)).Select(x => x.node).Where(x => x.allowedInSubGraph).ToArray();

            // Convert To Subgraph could create recursive reference loops if the target path already exists
            // Let's check for that here
            if (!string.IsNullOrEmpty(path))
            {
                if (GraphUtil.CheckForRecursiveDependencyOnPendingSave(path, nodes.OfType<SubGraphNode>(), "Convert To SubGraph"))
                    return;
            }

            graphObject.RegisterCompleteObjectUndo("Convert To Subgraph");

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
            var graphInputs = graphView.selection.OfType<SGBlackboardField>().Select(x => x.userData as ShaderInput);
            var categories = graphView.selection.OfType<SGBlackboardCategory>().Select(x => x.userData as CategoryData);

            // Collect the property nodes and get the corresponding properties
            var propertyNodes = graphView.selection.OfType<IShaderNodeView>().Where(x => (x.node is PropertyNode)).Select(x => ((PropertyNode)x.node).property);
            var metaProperties = graphView.graph.properties.Where(x => propertyNodes.Contains(x));

            // Collect the keyword nodes and get the corresponding keywords
            var keywordNodes = graphView.selection.OfType<IShaderNodeView>().Where(x => (x.node is KeywordNode)).Select(x => ((KeywordNode)x.node).keyword);
            var dropdownNodes = graphView.selection.OfType<IShaderNodeView>().Where(x => (x.node is DropdownNode)).Select(x => ((DropdownNode)x.node).dropdown);

            var metaKeywords = graphView.graph.keywords.Where(x => keywordNodes.Contains(x));
            var metaDropdowns = graphView.graph.dropdowns.Where(x => dropdownNodes.Contains(x));

            var copyPasteGraph = new CopyPasteGraph(graphView.selection.OfType<ShaderGroup>().Select(x => x.userData),
                nodes,
                graphView.selection.OfType<Edge>().Select(x => x.userData as Graphing.Edge),
                graphInputs,
                categories,
                metaProperties,
                metaKeywords,
                metaDropdowns,
                graphView.selection.OfType<StickyNote>().Select(x => x.userData),
                true,
                false);

            // why do we serialize and deserialize only to make copies of everything in the steps below?
            // is this just to clear out all non-serialized data?
            var deserialized = CopyPasteGraph.FromJson(MultiJson.Serialize(copyPasteGraph), graphView.graph);
            if (deserialized == null)
                return;

            var subGraph = new GraphData { isSubGraph = true, path = "Sub Graphs" };
            var subGraphOutputNode = new SubGraphOutputNode();
            {
                var drawState = subGraphOutputNode.drawState;
                drawState.position = new Rect(new Vector2(bounds.xMax + 200f, 0f), drawState.position.size);
                subGraphOutputNode.drawState = drawState;
            }
            subGraph.AddNode(subGraphOutputNode);
            subGraph.outputNode = subGraphOutputNode;

            // Always copy deserialized keyword inputs
            foreach (ShaderKeyword keyword in deserialized.metaKeywords)
            {
                var copiedInput = (ShaderKeyword)subGraph.AddCopyOfShaderInput(keyword);

                // Update the keyword nodes that depends on the copied keyword
                var dependentKeywordNodes = deserialized.GetNodes<KeywordNode>().Where(x => x.keyword == keyword);
                foreach (var node in dependentKeywordNodes)
                {
                    node.owner = graphView.graph;
                    node.keyword = copiedInput;
                }
            }

            // Always copy deserialized dropdown inputs
            foreach (ShaderDropdown dropdown in deserialized.metaDropdowns)
            {
                var copiedInput = (ShaderDropdown)subGraph.AddCopyOfShaderInput(dropdown);

                // Update the dropdown nodes that depends on the copied dropdown
                var dependentDropdownNodes = deserialized.GetNodes<DropdownNode>().Where(x => x.dropdown == dropdown);
                foreach (var node in dependentDropdownNodes)
                {
                    node.owner = graphView.graph;
                    node.dropdown = copiedInput;
                }
            }

            foreach (GroupData groupData in deserialized.groups)
            {
                subGraph.CreateGroup(groupData);
            }

            foreach (var node in deserialized.GetNodes<AbstractMaterialNode>())
            {
                var drawState = node.drawState;
                drawState.position = new Rect(drawState.position.position - middle, drawState.position.size);
                node.drawState = drawState;

                // Checking if the group guid is also being copied.
                // If not then nullify that guid
                if (node.group != null && !subGraph.groups.Contains(node.group))
                {
                    node.group = null;
                }

                subGraph.AddNode(node);
            }

            foreach (var note in deserialized.stickyNotes)
            {
                if (note.group != null && !subGraph.groups.Contains(note.group))
                {
                    note.group = null;
                }

                subGraph.AddStickyNote(note);
            }

            // figure out what needs remapping
            var externalOutputSlots = new List<Graphing.Edge>();
            var externalInputSlots = new List<Graphing.Edge>();
            var passthroughSlots = new List<Graphing.Edge>();
            foreach (var edge in deserialized.edges)
            {
                var outputSlot = edge.outputSlot;
                var inputSlot = edge.inputSlot;

                var outputSlotExistsInSubgraph = subGraph.ContainsNode(outputSlot.node);
                var inputSlotExistsInSubgraph = subGraph.ContainsNode(inputSlot.node);

                // pasting nice internal links!
                if (outputSlotExistsInSubgraph && inputSlotExistsInSubgraph)
                {
                    subGraph.Connect(outputSlot, inputSlot);
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
                else
                {
                    externalInputSlots.Add(edge);
                    externalOutputSlots.Add(edge);
                    passthroughSlots.Add(edge);
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

            var passthroughSlotRefLookup = new Dictionary<SlotReference, SlotReference>();

            var passedInProperties = new Dictionary<AbstractShaderProperty, AbstractShaderProperty>();
            foreach (var group in uniqueIncomingEdges)
            {
                var sr = group.slotRef;
                var fromNode = sr.node;
                var fromSlot = sr.slot;

                var materialGraph = graphObject.graph;
                var fromProperty = fromNode is PropertyNode fromPropertyNode
                    ? materialGraph.properties.FirstOrDefault(p => p == fromPropertyNode.property)
                    : null;

                AbstractShaderProperty prop;
                if (fromProperty != null && passedInProperties.TryGetValue(fromProperty, out prop))
                {
                }
                else
                {
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
                        case ConcreteSlotValueType.VirtualTexture:
                            prop = new VirtualTextureShaderProperty()
                            {
                                // also copy the VT settings over from the original property (if there is one)
                                value = (fromProperty as VirtualTextureShaderProperty)?.value ?? new SerializableVirtualTexture()
                            };
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    var propName = fromProperty != null
                        ? fromProperty.displayName
                        : fromSlot.concreteValueType.ToString();
                    prop.SetDisplayNameAndSanitizeForGraph(subGraph, propName);

                    subGraph.AddGraphInput(prop);
                    if (fromProperty != null)
                    {
                        passedInProperties.Add(fromProperty, prop);
                    }
                }

                var propNode = new PropertyNode();
                {
                    var drawState = propNode.drawState;
                    drawState.position = new Rect(new Vector2(bounds.xMin - 300f, 0f) + propPos,
                        drawState.position.size);
                    propPos += new Vector2(0, height);
                    propNode.drawState = drawState;
                }
                subGraph.AddNode(propNode);
                propNode.property = prop;


                Vector2 avg = Vector2.zero;
                foreach (var edge in group.edges)
                {
                    if (passthroughSlots.Contains(edge) && !passthroughSlotRefLookup.ContainsKey(sr))
                    {
                        passthroughSlotRefLookup.Add(sr, new SlotReference(propNode, PropertyNode.OutputSlotId));
                    }
                    else
                    {
                        subGraph.Connect(
                            new SlotReference(propNode, PropertyNode.OutputSlotId),
                            edge.inputSlot);

                        int i;
                        var inputs = edge.inputSlot.node.GetInputSlots<MaterialSlot>().ToList();

                        for (i = 0; i < inputs.Count; ++i)
                        {
                            if (inputs[i].slotReference.slotId == edge.inputSlot.slotId)
                            {
                                break;
                            }
                        }
                        avg += new Vector2(edge.inputSlot.node.drawState.position.xMin, edge.inputSlot.node.drawState.position.center.y + 30f * i);
                    }
                    //we collapse input properties so dont add edges that are already being added
                    if (!externalInputNeedingConnection.Any(x => x.Key.outputSlot.slot == edge.outputSlot.slot && x.Value == prop))
                    {
                        externalInputNeedingConnection.Add(new KeyValuePair<IEdge, AbstractShaderProperty>(edge, prop));
                    }
                }
                avg /= group.edges.Count;
                var pos = avg - new Vector2(150f, 0f);
                propNode.drawState = new DrawState()
                {
                    position = new Rect(pos, propNode.drawState.position.size),
                    expanded = propNode.drawState.expanded
                };
            }

            var uniqueOutgoingEdges = externalInputSlots.GroupBy(
                edge => edge.outputSlot,
                edge => edge,
                (key, edges) => new { slot = key, edges = edges.ToList() });

            var externalOutputsNeedingConnection = new List<KeyValuePair<IEdge, IEdge>>();
            foreach (var group in uniqueOutgoingEdges)
            {
                var outputNode = subGraph.outputNode as SubGraphOutputNode;

                AbstractMaterialNode node = group.edges[0].outputSlot.node;
                MaterialSlot slot = node.FindSlot<MaterialSlot>(group.edges[0].outputSlot.slotId);
                var slotId = outputNode.AddSlot(slot.concreteValueType);

                var inputSlotRef = new SlotReference(outputNode, slotId);

                foreach (var edge in group.edges)
                {
                    var newEdge = subGraph.Connect(passthroughSlotRefLookup.TryGetValue(edge.outputSlot, out SlotReference remap) ? remap : edge.outputSlot, inputSlotRef);
                    externalOutputsNeedingConnection.Add(new KeyValuePair<IEdge, IEdge>(edge, newEdge));
                }
            }

            if (FileUtilities.WriteShaderGraphToDisk(path, subGraph) != null)
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
            var firstNode = copyPasteGraph.GetNodes<AbstractMaterialNode>().FirstOrDefault();
            if (firstNode != null && copyPasteGraph.GetNodes<AbstractMaterialNode>().All(x => x.group == firstNode.group))
            {
                subGraphNode.group = firstNode.group;
            }

            subGraphNode.asset = loadedSubGraph;
            graphObject.graph.AddNode(subGraphNode);

            foreach (var edgeMap in externalInputNeedingConnection)
            {
                graphObject.graph.Connect(edgeMap.Key.outputSlot, new SlotReference(subGraphNode, edgeMap.Value.guid.GetHashCode()));
            }

            foreach (var edgeMap in externalOutputsNeedingConnection)
            {
                graphObject.graph.Connect(new SlotReference(subGraphNode, edgeMap.Value.inputSlot.slotId), edgeMap.Key.inputSlot);
            }

            graphObject.graph.RemoveElements(
                graphView.selection.OfType<IShaderNodeView>().Select(x => x.node).Where(x => !(x is PropertyNode || x is SubGraphOutputNode) && x.allowedInSubGraph).ToArray(),
                new IEdge[] { },
                new GroupData[] { },
                graphView.selection.OfType<StickyNote>().Select(x => x.userData).ToArray());

            List<GraphElement> moved = new List<GraphElement>();
            foreach (var nodeView in graphView.selection.OfType<IShaderNodeView>())
            {
                var node = nodeView.node;
                if (graphView.graph.removedNodes.Contains(node) || node is SubGraphOutputNode)
                {
                    continue;
                }

                var edges = graphView.graph.GetEdges(node);
                int numEdges = edges.Count();
                if (numEdges == 0)
                {
                    graphView.graph.RemoveNode(node);
                }
                else if (numEdges == 1 && edges.First().inputSlot.node != node) //its an output edge
                {
                    var edge = edges.First();
                    int i;
                    var inputs = edge.inputSlot.node.GetInputSlots<MaterialSlot>().ToList();
                    for (i = 0; i < inputs.Count; ++i)
                    {
                        if (inputs[i].slotReference.slotId == edge.inputSlot.slotId)
                        {
                            break;
                        }
                    }
                    node.drawState = new DrawState()
                    {
                        position = new Rect(new Vector2(edge.inputSlot.node.drawState.position.xMin, edge.inputSlot.node.drawState.position.center.y) - new Vector2(150f, -30f * i), node.drawState.position.size),
                        expanded = node.drawState.expanded
                    };
                    (nodeView as GraphElement).SetPosition(node.drawState.position);
                }
            }
            graphObject.graph.ValidateGraph();
        }

        public void Initialize(MaterialGraphEditWindow other)
        {
            // create a new window that copies the entire editor state of an existing window
            // this function is used to "reopen" an editor window that is closing, but where the user has canceled the close
            // for example, if the graph of a closing window was dirty, but could not be saved
            try
            {
                selectedGuid = other.selectedGuid;

                graphObject = CreateInstance<GraphObject>();
                graphObject.hideFlags = HideFlags.HideAndDontSave;
                graphObject.graph = other.graphObject.graph;

                graphObject.graph.messageManager = this.messageManager;

                UpdateTitle();

                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                m_HasError = true;
                m_GraphEditorView = null;
                graphObject = null;
                throw;
            }
        }

        private static readonly ProfilerMarker GraphLoadMarker = new ProfilerMarker("GraphLoad");
        private static readonly ProfilerMarker CreateGraphEditorViewMarker = new ProfilerMarker("CreateGraphEditorView");
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
                string graphName = Path.GetFileNameWithoutExtension(path);

                using (GraphLoadMarker.Auto())
                {
                    m_LastSerializedFileContents = File.ReadAllText(path, Encoding.UTF8);
                    graphObject = CreateInstance<GraphObject>();
                    graphObject.hideFlags = HideFlags.HideAndDontSave;
                    graphObject.graph = new GraphData
                    {
                        assetGuid = assetGuid,
                        isSubGraph = isSubGraph,
                        messageManager = messageManager
                    };
                    MultiJson.Deserialize(graphObject.graph, m_LastSerializedFileContents);
                    graphObject.graph.OnEnable();
                    graphObject.graph.ValidateGraph();
                }

                using (CreateGraphEditorViewMarker.Auto())
                {
                    graphEditorView = new GraphEditorView(this, m_GraphObject.graph, messageManager, graphName)
                    {
                        viewDataKey = selectedGuid,
                    };
                }

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

        // returns contents of the asset file, or null if any exception occurred
        private string ReadAssetFile()
        {
            var filePath = AssetDatabase.GUIDToAssetPath(selectedGuid);
            return FileUtilities.SafeReadAllText(filePath);
        }

        // returns true when the user is OK with closing the window or application (either they've saved dirty content, or are ok with losing it)
        // returns false when the user wants to cancel closing the window or application
        private bool PromptSaveIfDirtyOnQuit()
        {
            // only bother unless we've actually got data to preserve
            if (graphObject?.graph != null)
            {
                // if the asset has been deleted, ask the user what to do
                if (!AssetFileExists())
                    return DisplayDeletedFromDiskDialog(false);

                // If there are unsaved modifications, ask the user what to do.
                // If the editor has already handled this check we'll no longer have unsaved changes
                // (either they saved or they discarded, both of which will set hasUnsavedChanges to false).
                if (hasUnsavedChanges)
                {
                    int option = EditorUtility.DisplayDialogComplex(
                        "Shader Graph Has Been Modified",
                        GetSaveChangesMessage(),
                        "Save", "Cancel", "Discard Changes");

                    if (option == 0) // save
                    {
                        return SaveAsset();
                    }
                    else if (option == 1) // cancel (or escape/close dialog)
                    {
                        return false;
                    }
                    else if (option == 2) // discard
                    {
                        return true;
                    }
                }
            }
            return true;
        }

        private string GetSaveChangesMessage()
        {
            return "Do you want to save the changes you made in the Shader Graph?\n\n" +
                AssetDatabase.GUIDToAssetPath(selectedGuid) +
                "\n\nYour changes will be lost if you don't save them.";
        }

        public override void SaveChanges()
        {
            base.SaveChanges();
            SaveAsset();
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (graphEditorView == null)
                return;

            // this callback is only so we can run post-layout behaviors after the graph loads for the first time
            // we immediately unregister it so it doesn't get called again
            graphEditorView.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (m_FrameAllAfterLayout)
                graphEditorView.graphView.FrameAll();
            m_FrameAllAfterLayout = false;
        }
    }
}
