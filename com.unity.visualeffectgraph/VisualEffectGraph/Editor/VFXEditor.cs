using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace UnityEditor.Experimental
{
    public class VFXEditor : EditorWindow
    {
        public static readonly string ShaderDir = "/VFXShaders/Generated/";
        public static string GlobalShaderDir()  { return Application.dataPath + VFXEditor.ShaderDir; }
        public static string LocalShaderDir()   { return "Assets" + ShaderDir; }

        public static VFXEdResources Resources
        {
            get
            {
                if (s_Resources == null)
                    s_Resources = new VFXEdResources();
                return s_Resources;
            }
        }
        private static VFXEdResources s_Resources;

        public class VFXEdResources
        {
            public Texture2D DefaultSpriteTexture;

            public VFXEdResources()
            {
                DefaultSpriteTexture = (Texture2D)EditorGUIUtility.LoadRequired("DefaultParticle.tga");
            }
        }

        [MenuItem("VFXEditor/Export Skin")]
        public static void ExportSkin()
        {
            VFXEditor.styles.ExportGUISkin();
        }

        [MenuItem("Window/VFX Editor")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(VFXEditor));

            InitializeBlockLibrary();
            InitializeContextLibrary();
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

        public static VFX.VFXBlockLibrary BlockLibrary
        {
            get
            {
                InitializeBlockLibrary();
                return s_BlockLibrary;
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

        public static VFXGraph Graph { get { return s_Graph; }}

        public static void Log(string s) {
            return; // TMP Deactivate logging

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
        private static VFX.VFXBlockLibrary s_BlockLibrary;
        private static VFXContextLibraryCollection s_ContextLibrary;
		private static VFXGraph s_Graph;
        /* end Singletons */

        private VFXEdCanvas m_Canvas = null;
        private EditorWindow m_HostWindow = null;
        private Texture m_Icon = null;
        private VFXEdDataSource m_DataSource;

        private bool m_bShowHelp = false;

        private VFX.VFXBlockLibrary m_BlockLibrary;

        private static VFXComponent m_Component;
        private static VFXAsset s_Asset;

        public static Transform componentTransform
        {
            get 
            {
                if (component != null)
                    return component.gameObject.transform;
                return null;
            }
        }

        public static VFXComponent component { get { return m_Component; } }
        public static VFXAsset asset { get { return s_Asset; } }

        public static IEnumerable<VFXComponent> allComponents
        {
            get
            {
                if (asset == null)
                    return new List<VFXComponent>();

                VFXComponent[] vfxComponents = VFXComponent.GetAllActive();
                return vfxComponents.Where(vfx => vfx.vfxAsset == VFXEditor.asset);
            }
        }

        public static void ForeachComponents(Action<VFXComponent> action)
        {
            foreach (var component in allComponents)
                action(component);
        }

        private static void InitializeBlockLibrary()
        {
            if (s_BlockLibrary == null)
            {
                s_BlockLibrary = new VFX.VFXBlockLibrary();
                s_BlockLibrary.Load();
            }
        }

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;

            Selection.selectionChanged += OnSelectionChanged;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
            OnSelectionChanged(); // Call when enabled to retrieve the current selection

            s_Asset = m_CurrentAsset;
        }

        void OnDisable()
        {
            SetCurrentAsset(null);
            DestroyGraph();

            Selection.selectionChanged -= OnSelectionChanged;
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        private void SaveAsset()
        {
            if (m_CurrentAsset == null || s_Graph == null)
                return;

            m_CurrentAsset.XmlGraph = ModelSerializer.Serialize(s_Graph);
            Debug.Log("Set XML graph for " + m_CurrentAsset.name + " " + m_CurrentAsset.XmlGraph);

            EditorUtility.SetDirty(m_CurrentAsset);
            AssetDatabase.SaveAssets();

            s_Graph.systems.Invalidate(VFXElementModel.InvalidationCause.kParamChanged); // Needs to reload uniform once saved

            s_Graph.systems.Dirty = false;
            s_Graph.models.Dirty = false;
                
            Debug.Log("Save Asset");
        }

        private static void InitializeContextLibrary()
        {
            if (s_ContextLibrary == null)
            {
                s_ContextLibrary = new VFXContextLibraryCollection();
            }
        }

        private void InitializeCanvas()
        {
            if (m_Canvas == null)
            {
                m_DataSource = new VFXEdDataSource();
                m_Canvas = new VFXEdCanvas(this, m_HostWindow, m_DataSource);

                if (Graph != null)
                    m_DataSource.ResyncViews();
                m_Canvas.ReloadData();
                m_Canvas.Repaint();
            }



            if (m_Icon == null)
                m_Icon = EditorGUIUtility.Load("edicon.psd") as Texture;

            Undo.undoRedoPerformed -= OnUndoRedo;
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

            titleContent = new GUIContent("VFX Editor", m_Icon);
            
            if (Graph == null)
            {
                DrawNoAssetGUI();
                return;
            }

            DrawToolbar(new Rect(0, 0, position.width, EditorStyles.toolbar.fixedHeight));

            Rect canvasRect = new Rect(0, EditorStyles.toolbar.fixedHeight, position.width, position.height - EditorStyles.toolbar.fixedHeight);

            m_Canvas.OnGUI(this, canvasRect);

            if (m_NeedsCanvasReload)
            {
                if (VFXEditor.Graph != null)
                    m_DataSource.ResyncViews();
                m_Canvas.ReloadData();
                m_Canvas.Invalidate();
                m_Canvas.Layout();
                m_Canvas.FocusElements(false);
                m_Canvas.Repaint();
                m_NeedsCanvasReload = false;
                //Debug.Log(">>>>>>>>>>>>>>>> RELOAD CANVAS");
            }
            if(m_bShowHelp)
                ShowHelp(canvasRect);
        }

        bool m_EditBounds = false;
        void OnSceneGUI(SceneView sceneView)
        {
            if (m_EditBounds && asset != null)
            {
                ForeachComponents(c =>
                {
                    asset.bounds = VFXEdHandleUtility.BoxHandle(asset.bounds, c.transform.localToWorldMatrix);
                });
            }
        }

        private bool isOldPlaying = false;
        void Update()
        {
            // Handle the case when exiting play mode
            if (!Application.isPlaying && isOldPlaying)
            {
                m_Canvas.Clear();
                m_Canvas.Repaint();
                m_NeedsCanvasReload = true;

                if (Graph != null)
                    for (int i = 0; i < Graph.systems.GetNbChildren(); ++i)
                        Graph.systems.GetChild(i).Invalidate(VFXElementModel.InvalidationCause.kModelChanged);
            }
            isOldPlaying = Application.isPlaying;

            if (m_Component != null && m_CurrentAsset != m_Component.vfxAsset)
                SetCurrentAsset(m_Component.vfxAsset);

            if (Graph != null)
                Graph.systems.Update();
        }

        void OnDestroy()
        {
            SetCurrentAsset(null);

            /*UnityEngine.Object.DestroyImmediate(m_GameObject);
            m_GameObject = null;
            m_Component = null;*/

            s_BlockLibrary = null;
            s_ContextLibrary = null;

            ClearLog();
        }

        [SerializeField]
        private VFXAsset m_CurrentAsset;

        public void DestroyGraph()
        {
            // Remove systems
            if (s_Graph != null)
            {
                /*for (int i = 0; i < s_Graph.systems.GetNbChildren(); ++i)
                    s_Graph.systems.GetChild(i).RemoveSystem();*/
                s_Graph.systems.Dispose();
                s_Graph = null;
            }

            if (m_DataSource != null)
            {
                m_DataSource.ClearUI();
                m_NeedsCanvasReload = true;
            }
        }

        private bool m_NeedsCanvasReload = false;
        public void SetCurrentAsset(VFXAsset asset,bool force = false)
        {
            // Remove any edited asset when application is playing
            if (Application.isPlaying)
                asset = null;

            if (m_CurrentAsset != asset || s_Graph == null || force) 
            {
                // Made all systems visible
                if (s_Graph != null)
                {
                    ForeachComponents(c =>
                    {
                        for (int i = 0; i < s_Graph.systems.GetNbChildren(); ++i)
                            c.SetHidden(s_Graph.systems.GetChild(i).Id, false);
                    });
                }

                if (m_CurrentAsset != null)
                {
                    SaveAsset();
                    DestroyGraph();
                }

                m_CurrentAsset = asset;
                s_Asset = m_CurrentAsset;
                if (m_CurrentAsset != null)
                {
                    // Unselect component if it is not this asset
                    if (component != null && component.vfxAsset != m_CurrentAsset)
                        SetCurrentComponent(null);

                    //Debug.Log("------------------------ CREATE NEW GRAPH: " + asset.ToString()); 
                    string xml = m_CurrentAsset.XmlGraph;
                    Debug.Log("Get XML graph from " + m_CurrentAsset.name + " " + m_CurrentAsset.XmlGraph);

                    // Remove all previous systems as the Ids may have changed
                    m_CurrentAsset.RemoveAllSystems();
                    ForeachComponents(c => c.RemoveAllSystems());
                    //RemoveGeneratedShaders(m_CurrentAsset);

                    VFXSystemModel.ReinitIDsProvider();
                    s_Graph = ModelSerializer.Deserialize(xml);
                    for (int i = 0; i < s_Graph.systems.GetNbChildren(); ++i)
                        s_Graph.systems.GetChild(i).Invalidate(VFXElementModel.InvalidationCause.kModelChanged);  
                }
                else
                {
                    //Debug.Log("------------------------ SET NULL GRAPH");
                    DestroyGraph();
                    SetCurrentComponent(null);
                }
            }

            if (m_Canvas != null)
            {
                //Debug.Log(">>>>>>>>>>>>>>>> CLEAR CANVAS");
                m_Canvas.Clear();
                m_Canvas.Repaint();    
            }
            m_NeedsCanvasReload = true;
        }

        private void SetCurrentComponent(VFXComponent c)
        {
            if (m_Component != null)
                ReinitComponentPlayControls(m_Component);

            m_Component = c;
        }

        private void OnSelectionChanged()
        {
            var activeGo = Selection.activeGameObject;
            if (activeGo != null)
            {
                var vfxComponent = activeGo.GetComponent<VFXComponent>();
                if (vfxComponent != null && m_Component != vfxComponent)
                {
                    SetCurrentComponent(vfxComponent);
                    SetCurrentAsset(vfxComponent.vfxAsset);
                }
            }
            else
            {
                var assets = Selection.assetGUIDs;
                if (assets.Length == 1)
                {
                    var selected = AssetDatabase.LoadAssetAtPath<VFXAsset>(AssetDatabase.GUIDToAssetPath(assets[0]));
                    if (selected != null)
                        SetCurrentAsset(selected);
                }
            }
        }

        private void SetPlayRate(object rate)
        {
            component.playRate = (float)rate;
        }

        void DrawNoAssetGUI()
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.Label("NO ASSET SELECTED", styles.EventNodeText);
                        GUILayout.Label("Please Create or Select a VFX Asset in your project view, or select a VFXComponent with a valid asset to edit in this view.", styles.CanvasHelpText);
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
            }
        }

        void DrawToolbar(Rect rect)
        {
            GUI.BeginGroup(rect);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (m_CurrentAsset != null)
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                    SaveAsset();
            }

            if(GUILayout.Button("Select...", EditorStyles.toolbarDropDown))
            {
                GenericMenu selectMenu = new GenericMenu();

                selectMenu.AddItem(new GUIContent("Asset in Project"), false, () => { Selection.objects = new UnityEngine.Object[] { asset };});
                
                
                if (component != null)
                {
                    selectMenu.AddItem(new GUIContent("Last Component in Scene"), false, () => { Selection.objects = new UnityEngine.Object[] { component.gameObject }; });
                }

                selectMenu.AddItem(new GUIContent("All Asset Instances in Scene"), false, () => 
                {
                    List<GameObject> gameObjects = new List<GameObject>();
                    VFXEditor.ForeachComponents(c => gameObjects.Add(c.gameObject));
                    Selection.objects = gameObjects.ToArray();
                });
                
                selectMenu.ShowAsContext();
            }

            GUILayout.Space(40);


            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarRestart), EditorStyles.toolbarButton))
            {
                foreach (var c in allComponents)
                {
                    c.pause = false;
                    c.Reinit();
                }
            }

            EditorGUI.BeginDisabledGroup(component == null);

            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarPlay), EditorStyles.toolbarButton))
                component.pause = false;

            if (component != null)
                component.pause = GUILayout.Toggle(component.pause, new GUIContent(VFXEditor.styles.ToolbarPause), EditorStyles.toolbarButton);
            else
                GUILayout.Toggle(false, new GUIContent(VFXEditor.styles.ToolbarPause), EditorStyles.toolbarButton);

            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarStop), EditorStyles.toolbarButton))
            {
                component.pause = true;
                component.Reinit();
            }

            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarFrameAdvance), EditorStyles.toolbarButton))
            {
                component.pause = true;
                component.AdvanceOneFrame();
            }

            if (GUILayout.Button("PlayRate", EditorStyles.toolbarDropDown))
            {
                GenericMenu toolsMenu = new GenericMenu();
                float rate = component.playRate;
                toolsMenu.AddItem(new GUIContent("800%"), rate == 8.0f, SetPlayRate, 8.0f);
                toolsMenu.AddItem(new GUIContent("200%"), rate == 2.0f, SetPlayRate, 2.0f);
                toolsMenu.AddItem(new GUIContent("100% (RealTime)"), rate == 1.0f, SetPlayRate, 1.0f);
                toolsMenu.AddItem(new GUIContent("50%"), rate == 0.5f, SetPlayRate, 0.5f);
                toolsMenu.AddItem(new GUIContent("25%"), rate == 0.25f, SetPlayRate, 0.25f);
                toolsMenu.AddItem(new GUIContent("10%"), rate == 0.1f, SetPlayRate, 0.1f);
                toolsMenu.AddItem(new GUIContent("1%"), rate == 0.01f, SetPlayRate, 0.01f);

                toolsMenu.DropDown(new Rect(0, 0, 0, 16));
                EditorGUIUtility.ExitGUI();
            }

            if (component != null)
            {
                float r = component.playRate;
                float nr = Mathf.Pow(GUILayout.HorizontalSlider(Mathf.Sqrt(component.playRate), 0.0f, Mathf.Sqrt(8.0f), GUILayout.Width(140.0f)), 2.0f);
                GUILayout.Label(Mathf.Round(nr * 100) + "%", GUILayout.Width(50.0f));
                if (r != nr)
                    SetPlayRate(nr);
            }
            else
            {
                GUILayout.HorizontalSlider(0.0f, 0.0f, 1.0f, GUILayout.Width(140.0f));
                GUILayout.Space(50.0f);
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            GUILayout.Label("Editing: " + AssetDatabase.GetAssetPath(m_CurrentAsset) + (s_Graph.systems.Dirty || s_Graph.models.Dirty ? "*" : ""), EditorStyles.toolbarButton);
            m_EditBounds = GUILayout.Toggle(m_EditBounds, "Edit Bounds", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            //bool UsePhaseShift = Graph.systems.PhaseShift;
            //Graph.systems.PhaseShift = GUILayout.Toggle(UsePhaseShift, UsePhaseShift ? "With Sampling Correction" : "No Sampling Correction", EditorStyles.toolbarButton);

            m_bShowHelp = GUILayout.Toggle(m_bShowHelp, "Help", EditorStyles.toolbarButton);

            GUILayout.EndHorizontal();
            GUI.EndGroup();

        }

        private void ReinitComponentPlayControls(VFXComponent c)
        {
            c.pause = false;
            c.playRate = 1.0f;
        }

        private void RemoveGeneratedShaders(VFXAsset asset)
        {
            string assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(VFXEditor.asset));
            if (assetGuid.Length == 0) // To avoid erasing all the shaders if the asset is not found
                return;

            string[] guids = AssetDatabase.FindAssets(assetGuid, new string[] { "Assets/VFXEditor/Generated" });
            foreach (var guid in guids)
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

            if (guids.Length > 0)
                Debug.Log("Removed " + guids.Length + " old VFX shaders for asset "+assetGuid);
        }

        #region TOOL WINDOWS
        void ShowHelp(Rect canvasRect)
        {
            GUI.Label(canvasRect, @"<size=21><b>VFX Editor Controls</b></size>

<b> * Left Mouse Button:</b> Select nodes, connect
<b> * Drag Left Mouse (in canvas):</b> Multiple Selection
<b> * Drag Left Mouse (nodeblocks):</b> Move / Reorder Nodeblocks
<b> * Right Mouse Button:</b> Contextual Menu
<b> * Shift + Hold Right Mouse Button</b> Show debug info panel
<b> * Mouse Wheel:</b> Zoom In/Out

<b> * [TAB] Key:</b> Pop-up tab menu (contextual)
<b> * [DEL] Key:</b> Delete Selected
<b> * [D] Key:</b> Refreshes canvas
<b> * [L] Key (with node selected):</b> Layout system
<b> * [F] Key :</b> Focus on Selection
<b> * [A] Key :</b> Focus on All Systems

<b> * [Space] Key:</b> Restart Effect
<b> * [1,2,3,4,5] Key:</b> Change Simulation Speed
<b> * [P] Key:</b> Advance one frame
", VFXEditor.styles.CanvasHelpText);
        }
        #endregion

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
            m_Contexts.Add(new VFXParticleUpdate());
            m_Contexts.Add(new VFXBasicOutput());
            m_Contexts.Add(new VFXPointOutputDesc());
            m_Contexts.Add(new VFXBillboardOutputDesc());
            m_Contexts.Add(new VFXMorphSubUVBillboardOutputDesc());
            m_Contexts.Add(new VFXQuadAlongVelocityOutputDesc());
            m_Contexts.Add(new VFXQuadRotateAxisOutputDesc());
            m_Contexts.Add(new VFXQuadFixedOrientationOutputDesc());

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
