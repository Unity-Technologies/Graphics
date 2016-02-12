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

        public static VFXEditorStyles styles
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

        private static VFXEditorMetrics s_Metrics;
        private static VFXEditorStyles s_Styles;
        private static VFXBlockLibraryCollection s_BlockLibrary;
        /* end Singletons */

        private VFXEdCanvas m_Canvas = null;
        private EditorWindow m_HostWindow = null;
        private Texture m_Icon = null;
        private Rect m_LibraryRect;
        private Rect m_PreviewRect;
        private VFXEdDataSource m_DataSource;

        private bool m_bShowPreview = false;
        private bool m_bShowLibrary = false;
        private bool m_CannotPreview = true;
        private VFXAsset m_CurrentAsset;

        private VFXBlockLibraryCollection m_BlockLibrary;

        private static void InitializeBlockLibrary()
        {
            if (s_BlockLibrary == null)
            {
                s_BlockLibrary = new VFXBlockLibraryCollection();
                s_BlockLibrary.Load();
            }
        }

        private void InitializeCanvas()
        {
            if (m_Canvas == null)
            {
                m_DataSource = ScriptableObject.CreateInstance<VFXEdDataSource>();
                m_Canvas = new VFXEdCanvas(this, m_HostWindow, m_DataSource);

                // draggable manipulator allows to move the canvas around. Note that individual elements can have the draggable manipulator on themselves
                m_Canvas.AddManipulator(new Draggable(2, EventModifiers.None));
                m_Canvas.AddManipulator(new Draggable(0, EventModifiers.Alt));

                // make the canvas zoomable
                m_Canvas.AddManipulator(new Zoomable(Zoomable.ZoomType.AroundMouse));

                // allow framing the selection when hitting "F" (frame) or "A" (all). Basically shows how to trap a key and work with the canvas selection
                m_Canvas.AddManipulator(new Frame(Frame.FrameType.All));
                m_Canvas.AddManipulator(new Frame(Frame.FrameType.Selection));

                // The following manipulator show how to work with canvas2d overlay and background rendering
                m_Canvas.AddManipulator(new RectangleSelect());
                m_Canvas.AddManipulator(new ScreenSpaceGrid());
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

        void OnSelectionChange()
        {
            if (Selection.activeObject != null)
                if (Selection.activeObject.GetType() == typeof(VFXAsset))
                    Debug.Log("Selection Changed : " + Selection.activeObject);
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

            titleContent = new GUIContent("VFX Editor", m_Icon);
            //GUI.Toolbar(new Rect(0, 0, position.width, 24),0);
            DrawToolbar(new Rect(0, 0, position.width, EditorStyles.toolbar.fixedHeight));
            Rect canvasRect = new Rect(0, EditorStyles.toolbar.fixedHeight, position.width, position.height - EditorStyles.toolbar.fixedHeight);
            m_Canvas.OnGUI(this, canvasRect);
            DrawWindows(canvasRect);
        }


        void DrawToolbar(Rect rect)
        {
            GUI.BeginGroup(rect);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Create...", EditorStyles.toolbarButton))
            {

            }
            GUILayout.FlexibleSpace();
            m_Canvas.showQuadTree = GUILayout.Toggle(m_Canvas.showQuadTree, "Canvas2D Debug Info", EditorStyles.toolbarButton);
            if (GUILayout.Button("Refresh Canvas", EditorStyles.toolbarButton))
            {
                m_Canvas.DeepInvalidate();
                m_Canvas.Repaint();
            }
            if (GUILayout.Button("Reload Library", EditorStyles.toolbarButton))
            {
                BlockLibrary.Load();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
            {
                m_Canvas = null;
                InitializeCanvas();
                s_BlockLibrary = null;
            }
            if (GUILayout.Button("Tools", EditorStyles.toolbarDropDown))
            {
                GenericMenu toolsMenu = new GenericMenu();
                if (Selection.activeGameObject != null)
                    toolsMenu.AddItem(new GUIContent("Optimize Selected"), false, null);
                else
                    toolsMenu.AddDisabledItem(new GUIContent("Optimize Selected"));
                toolsMenu.AddSeparator("");
                toolsMenu.AddItem(new GUIContent("Help..."), false, null);
                // Offset menu from right of editor window
                toolsMenu.DropDown(new Rect(Screen.width - 216 - 40, 0, 0, 16));
                EditorGUIUtility.ExitGUI();
            }

            m_bShowLibrary = GUILayout.Toggle(m_bShowLibrary, "NodeBlock Library", EditorStyles.toolbarButton);
            m_bShowPreview = GUILayout.Toggle(m_bShowPreview, "Preview", EditorStyles.toolbarButton);

            GUILayout.EndHorizontal();
            GUI.EndGroup();

        }

        #region TOOL WINDOWS
        void DrawWindows(Rect canvasRect)
        {
            // Calculate Rect's
            m_LibraryRect = new Rect(
                                                canvasRect.xMax - (VFXEditorMetrics.LibraryWindowWidth + 2 * VFXEditorMetrics.WindowPadding),
                                                canvasRect.yMin + VFXEditorMetrics.WindowPadding,
                                                VFXEditorMetrics.LibraryWindowWidth,
                                                canvasRect.height - (4 * VFXEditorMetrics.WindowPadding)
                                            );

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
            if (m_bShowLibrary)
                GUI.Window(0, m_LibraryRect, DrawLibraryWindowContent, "NodeBlock Library");
            if (m_bShowPreview)
                GUI.Window(1, m_PreviewRect, DrawPreviewWindowContent, "Preview");
            EndWindows();
        }
        void DrawLibraryWindowContent(int windowID)
        {
            GUILayout.BeginScrollView(Vector2.zero);

            for (int i = 0; i < 10; i++)
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Box("Test Item " + i.ToString(), EditorStyles.toolbarButton);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
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

                    /*Debug.Log("Found block: " + block.m_Name + " " + block.m_Params.Length);
					for (int k = 0; k < block.m_Params.Length; ++k)
					{
						//Debug.Log("\t" + block.m_Params[k]);
						//Debug.Log("\t" + block.m_Params[k].m_Name);
					}*/
                }
            }

            Debug.Log("Reload VFXBlock libraries. Found " + guids.Length + " libraries with a total of " + m_Blocks.Count + " blocks");
        }

        // Just for test
        public VFXBlock GetRandomBlock()
        {
            if (m_Blocks.Count > 0)
            {
                int index = Random.Range(0, m_Blocks.Count);
                return m_Blocks[index];
            }
            else
            {
                VFXBlock block = new VFXBlock();
                block.m_Name = "EmptyNode";
                block.m_Params = new VFXParam[0];
                return block;
            }
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


}
