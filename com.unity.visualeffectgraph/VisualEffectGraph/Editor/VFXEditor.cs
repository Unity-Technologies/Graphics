using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace UnityEditor.Experimental
{
    public class VFXEditor : EditorWindow
    {

        [MenuItem("VFXEditor/Export Skin")]
        public static void ExportSkin()
        {
            VFXEditor.styles.ExportGUISkin();
        }

        [MenuItem("Assets/Create/VFX Asset")]
        public static void CreateVFXAsset()
        {
            VFXAsset asset = ScriptableObject.CreateInstance<VFXAsset>();

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New VFX Asset.asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

        }

        [MenuItem("Window/VFX Editor %R")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(VFXEditor));
        }

        /* Singletons */
        public static VFXEditorMetrics metrics
        {
            get
            {
                if (s_Metrics == null)
                    s_Metrics = new VFXEditorMetrics();
                return s_Metrics;
            }
        }

        internal static VFXEditorStyles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new VFXEditorStyles();
                return s_Styles;
            }
        }

        public static VFXBlockLibraryCollection BlockLibrary
        {
            get
            {
                InitializeBlockLibrary();
                return s_BlockLibrary;
            }
        }

        public static VFXDataBlockLibraryCollection DataBlockLibrary
        {
            get
            {
                InitializeDataBlockLibrary();
                return s_DataBlockLibrary;
            }
        }

        public static VFXContextLibraryCollection ContextLibrary
        {
            get
            {
                InitializeContextLibrary();
                return s_ContextLibrary;
            }
        }

		public static VFXAssetModel AssetModel
		{
			get
			{
				if (s_AssetModel == null)
					s_AssetModel = new VFXAssetModel();
				return s_AssetModel;
			}
		}

        public static VFXEdSpawnTemplateLibrary SpawnTemplates
        {
            get
            {
                InitializeSpawnTemplateLibrary();
                return s_SpawnTemplates;
            }
        }

        // DEBUG OUTPUT
        public static void Log(string s) {
            int currentIndex = DebugLines.Count - 1;

            if (currentIndex == -1)
            {
                DebugLines.Add("");
                ++currentIndex;
            }

            string currentStr = DebugLines[currentIndex];

            if (currentStr.Length + s.Length > 16384 - 1) // Max number handled for single string rendering (due to 16 bit index buffer and no automatic splitting)
            {
                // TODO Dont handle the case where there s more than 16384 char for s
                ++currentIndex;
                currentStr = "";
                DebugLines.Add(currentStr);
            }

            currentStr += s;
            currentStr += "\n";
            DebugLines[currentIndex] = currentStr;
        }

        public static void ClearLog() {
            DebugLines = new List<string>();
        }
        private static List<string> DebugLines = new List<string>();
        private static List<string> GetDebugOutput() {
            return DebugLines;       
        }
        // END DEBUG OUTPUT

        private static VFXEditorMetrics s_Metrics;
        private static VFXEditorStyles s_Styles;
        private static VFXBlockLibraryCollection s_BlockLibrary;
        private static VFXDataBlockLibraryCollection s_DataBlockLibrary;
        private static VFXContextLibraryCollection s_ContextLibrary;
		private static VFXAssetModel s_AssetModel;

        private static VFXEdSpawnTemplateLibrary s_SpawnTemplates;
        /* end Singletons */

        private VFXEdCanvas m_Canvas = null;
        private EditorWindow m_HostWindow = null;
        private Texture m_Icon = null;
        private Rect m_LibraryRect;
        private Rect m_PreviewRect;
        private VFXEdDataSource m_DataSource;

        private bool m_bShowPreview = false;
        private bool m_CannotPreview = false;
        private VFXAsset m_CurrentAsset;

        private bool m_bShowDebug = false;
        private int m_ShowDebugPage = 0;
        private string m_NewTemplateCategory = "";
        private string m_NewTemplateName = "";

        private Vector2 m_DebugLogScroll = Vector2.zero;


        private VFXBlockLibraryCollection m_BlockLibrary;

        private static void InitializeBlockLibrary()
        {
            if (s_BlockLibrary == null)
            {
                s_BlockLibrary = new VFXBlockLibraryCollection();
                s_BlockLibrary.Load();
            }
        }

        private static void InitializeDataBlockLibrary()
        {
            if (s_DataBlockLibrary == null)
            {
                s_DataBlockLibrary = new VFXDataBlockLibraryCollection();
                s_DataBlockLibrary.Load();
            }
        }

        private static void InitializeContextLibrary()
        {
            if (s_ContextLibrary == null)
            {
                s_ContextLibrary = new VFXContextLibraryCollection();
            }
        }

        private static void InitializeSpawnTemplateLibrary()
        {
            if (s_SpawnTemplates == null)
            {
                s_SpawnTemplates = VFXEdSpawnTemplateLibrary.Create();
            }
        }

        private void InitializeCanvas()
        {
            if (m_Canvas == null)
            {
                m_DataSource = ScriptableObject.CreateInstance<VFXEdDataSource>();
                m_Canvas = new VFXEdCanvas(this, m_HostWindow, m_DataSource);
            }

            if (m_Icon == null)
                m_Icon = EditorGUIUtility.Load("edicon.psd") as Texture;

            Undo.undoRedoPerformed += OnUndoRedo;

            Rebuild();
        }

        void OnUndoRedo()
        {
            m_Canvas.ReloadData();
            m_Canvas.Repaint();
        }

        private void Rebuild()
        {
            if (m_Canvas == null)
                return;

            m_Canvas.Clear();
            m_Canvas.ReloadData();
            m_Canvas.ZSort();
        }

        void OnGUI()
        {
            m_HostWindow = this;

            if (s_BlockLibrary == null)
                InitializeBlockLibrary();

            if (m_Canvas == null)
            {
                InitializeCanvas();
            }

            AssetModel.Update();

            titleContent = new GUIContent("VFX Editor", m_Icon);

            DrawToolbar(new Rect(0, 0, position.width, EditorStyles.toolbar.fixedHeight));


            Rect canvasRect;
            
            if(m_bShowDebug)
            {
                GUILayout.BeginArea(new Rect(position.width-VFXEditorMetrics.DebugWindowWidth, EditorStyles.toolbar.fixedHeight, VFXEditorMetrics.DebugWindowWidth, position.height -EditorStyles.toolbar.fixedHeight));
                GUILayout.BeginVertical();

                
                // Debug Window Toolbar
                GUILayout.BeginHorizontal(EditorStyles.toolbar);

                GUI.color = Color.green * 4;
                GUILayout.Label("Canvas2D : ",EditorStyles.toolbarButton);
                GUI.color = Color.white;
                m_Canvas.showQuadTree = GUILayout.Toggle(m_Canvas.showQuadTree, "Debug", EditorStyles.toolbarButton);
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    m_Canvas.DeepInvalidate();
                    m_Canvas.Repaint();
                }

                GUILayout.FlexibleSpace();
                GUI.color = Color.yellow * 4;
                GUILayout.Label("VFXEditor :",EditorStyles.toolbarButton);
                GUI.color = Color.white;

                if (GUILayout.Button("Reload Library", EditorStyles.toolbarButton))
                {
                    BlockLibrary.Load();
                }

                if (GUILayout.Button("Clear Log", EditorStyles.toolbarButton))
                    ClearLog();

                GUILayout.EndHorizontal();

                // Tabs Toolbar
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label("Choose Page : ", EditorStyles.toolbarButton);
                if(GUILayout.Button("Debug Log", EditorStyles.toolbarButton)) m_ShowDebugPage = 0;
                if(GUILayout.Button("Edit Templates", EditorStyles.toolbarButton)) m_ShowDebugPage = 1;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();


                m_DebugLogScroll = GUILayout.BeginScrollView(m_DebugLogScroll, false, true);

                switch(m_ShowDebugPage)
                {
                    case 0: // Debug log
                    {                  
                        List<string> debugOutput = VFXEditor.GetDebugOutput();
                        foreach (string str in debugOutput)
                            GUILayout.Label(str);
                        break;
                    }

                    case 1: // Edit Templates
                    {
                        EditorGUI.indentLevel++;
                        GUILayout.Space(16.0f);
                        GUILayout.Label("Add New Template from Selection...", VFXEditor.styles.InspectorHeader);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Category : ");
                        m_NewTemplateCategory = GUILayout.TextField(m_NewTemplateCategory, 150);
                        GUILayout.Label("Name : ");
                        m_NewTemplateName = GUILayout.TextField(m_NewTemplateName, 150);
                        if (GUILayout.Button("Add..."))
                        {
                            VFXEdSpawnTemplate t = VFXEdSpawnTemplateLibrary.CreateTemplateFromSelection(m_Canvas, m_NewTemplateCategory, m_NewTemplateName);
                            if (t != null)
                            {
                                VFXEditor.SpawnTemplates.AddTemplate(t);
                                SpawnTemplates.WriteLibrary();
                                m_NewTemplateCategory = "";
                                m_NewTemplateName = "";
                            }

                        }
                        GUILayout.EndHorizontal();
                        GUILayout.Space(16.0f);
                        GUILayout.Label("Currently Loaded Templates", VFXEditor.styles.InspectorHeader);

                        List<string> todelete = new List<string>();
                        foreach (VFXEdSpawnTemplate t in SpawnTemplates.Templates)
                        {
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button("X"))
                            {
                                todelete.Add(t.Path);
                            }
                            GUILayout.Label(t.Path);
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                        // If Has to delete...
                        if (todelete.Count > 0) foreach (string s in todelete) SpawnTemplates.DeleteTemplate(s);


                        GUILayout.Space(16.0f);
                        GUILayout.Label("Debug...", VFXEditor.styles.InspectorHeader);
                        if (GUILayout.Button("Fill templates (debug)"))
                        {
                            SpawnTemplates.Initialize();
                        }

                        if (GUILayout.Button("Reload Templates"))
                        {
                            SpawnTemplates.ReloadLibrary();
                        }
                        EditorGUI.indentLevel--;
                        break;
                    }

                    default: break;
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUILayout.EndArea();
                canvasRect = new Rect(0, EditorStyles.toolbar.fixedHeight, position.width-VFXEditorMetrics.DebugWindowWidth, position.height - EditorStyles.toolbar.fixedHeight);
            }
            else
            {
                canvasRect = new Rect(0, EditorStyles.toolbar.fixedHeight, position.width, position.height - EditorStyles.toolbar.fixedHeight);
            }

            m_Canvas.OnGUI(this, canvasRect);
            DrawWindows(canvasRect);
        }

        void OnDestroy()
        {
            s_BlockLibrary = null;
            s_DataBlockLibrary = null;
            s_ContextLibrary = null;
            s_SpawnTemplates = null;
            
            s_AssetModel.Dispose();
            s_AssetModel = null;
            
            ClearLog();
        }

        void DrawToolbar(Rect rect)
        {
            GUI.BeginGroup(rect);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            // TODO : Add ifs to control effect
            GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarRestart), EditorStyles.toolbarButton);
            GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarPlay), EditorStyles.toolbarButton);
            GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarPause), EditorStyles.toolbarButton);
            GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarStop), EditorStyles.toolbarButton);
            GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarFrameAdvance), EditorStyles.toolbarButton);
            if (GUILayout.Button("PlayRate", EditorStyles.toolbarDropDown))
            {
                GenericMenu toolsMenu = new GenericMenu();
                // TODO : Change null's to callbacks to set playrate
                toolsMenu.AddItem(new GUIContent("100% (RealTime)"),false, null);
                toolsMenu.AddItem(new GUIContent("50%" ),false, null);
                toolsMenu.AddItem(new GUIContent("25%"),false, null);
                toolsMenu.AddItem(new GUIContent("10%"),false, null);
                toolsMenu.AddItem(new GUIContent("1%"),false, null);

                toolsMenu.DropDown(new Rect(0, 0, 0, 16));
                EditorGUIUtility.ExitGUI();
            }

            GUILayout.FlexibleSpace();

            bool UsePhaseShift = AssetModel.PhaseShift;
            AssetModel.PhaseShift = GUILayout.Toggle(UsePhaseShift, UsePhaseShift ? "With Sampling Correction" : "No Sampling Correction", EditorStyles.toolbarButton);

            m_bShowDebug = GUILayout.Toggle(m_bShowDebug, "DEBUG PANEL", EditorStyles.toolbarButton);
            m_bShowPreview = GUILayout.Toggle(m_bShowPreview, "Preview", EditorStyles.toolbarButton);

            GUILayout.EndHorizontal();
            GUI.EndGroup();

        }

        #region TOOL WINDOWS
        void DrawWindows(Rect canvasRect)
        {
            // Calculate Rect's

            m_PreviewRect = new Rect(
                                            canvasRect.xMax - (VFXEditorMetrics.PreviewWindowWidth + 2 * VFXEditorMetrics.WindowPadding),
                                            canvasRect.yMax - (VFXEditorMetrics.PreviewWindowHeight + 2 * VFXEditorMetrics.WindowPadding),
                                            VFXEditorMetrics.PreviewWindowWidth,
                                            VFXEditorMetrics.PreviewWindowHeight
                                       );


            if (m_bShowPreview)
            {
                m_LibraryRect.height = canvasRect.height - VFXEditorMetrics.PreviewWindowHeight - (5 * VFXEditorMetrics.WindowPadding);
            }

            BeginWindows();
            if (m_bShowPreview)
                GUI.Window(0, m_PreviewRect, DrawPreviewWindowContent, "Preview");
            EndWindows();
        }


        void DrawPreviewWindowContent(int windowID)
        {
            if (m_CannotPreview)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("  No Preview Available    ", MessageType.Error);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }
        #endregion

    }


    public class VFXBlockLibraryCollection
    {
        private List<VFXBlock> m_Blocks;
        
        public VFXBlockLibraryCollection()
        {
            m_Blocks = new List<VFXBlock>();
        }

        public void Load()
        {
            AssetDatabase.Refresh();
            m_Blocks.Clear();

            string[] guids = AssetDatabase.FindAssets("t:VFXBlockLibrary");
            VFXBlockLibrary[] blockLibraries = new VFXBlockLibrary[guids.Length];
            //Debug.Log("Found " + guids.Length + " VFXBlockLibrary assets");

            for (int i = 0; i < guids.Length; ++i)
            {
                blockLibraries[i] = AssetDatabase.LoadAssetAtPath<VFXBlockLibrary>(AssetDatabase.GUIDToAssetPath(guids[i]));
                //Debug.Log("Found " + blockLibraries[i].GetNbBlocks() + " VFXBlocks in library " + i);
                for (int j = 0; j < blockLibraries[i].GetNbBlocks(); ++j)
                {
                    VFXBlock block = blockLibraries[i].GetBlock(j);
                    m_Blocks.Add(block);

                }
            }

            // Debug.Log("Reload VFXBlock libraries. Found " + guids.Length + " libraries with a total of " + m_Blocks.Count + " blocks");
        }

        public VFXBlock GetBlock(string name)
        {
            return m_Blocks.Find(block => block.m_Name.Equals(name));
        }

        public ReadOnlyCollection<VFXBlock> GetBlocks()
        {
            return new ReadOnlyCollection<VFXBlock>(m_Blocks);
        }
    }

    public class VFXContextLibraryCollection
    {
        private List<VFXContextDesc> m_Contexts;

        public VFXContextLibraryCollection()
        {
            m_Contexts = new List<VFXContextDesc>();

            // Register context here
            m_Contexts.Add(new VFXBasicInitialize());
            m_Contexts.Add(new VFXBasicUpdate());
            m_Contexts.Add(new VFXBasicOutput());
            m_Contexts.Add(new VFXPointOutputDesc());
            m_Contexts.Add(new VFXBillboardOutputDesc());
            m_Contexts.Add(new VFXQuadAlongVelocityOutputDesc());
        }

        public VFXContextDesc GetContext(string name)
        {
            return m_Contexts.Find(context => context.Name.Equals(name));
        }

        public ReadOnlyCollection<VFXContextDesc> GetContexts()
        {
            return new ReadOnlyCollection<VFXContextDesc>(m_Contexts);
        }
    }
}
