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
            if (m_Deleted || graphObject.graph == null)
                return false; // Not dirty; it's gone.

            var currentJson = MultiJson.Serialize(graphObject.graph);
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
                graphObject.graph = MultiJson.Deserialize<GraphData>(textGraph);
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
