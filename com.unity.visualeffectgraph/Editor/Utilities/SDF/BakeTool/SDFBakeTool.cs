using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using UnityEngine.VFX.SDF;


namespace UnityEditor.VFX.SDF
{
    class SDFBakeTool : EditorWindow
    {
        [MenuItem("Window/Visual Effects/Utilities/SDF Bake Tool", false, 3013)]
        static void OpenWindow()
        {
            GetWindow<SDFBakeTool>();
        }

        [SerializeField]
        private SdfBakerSettings m_Settings;

        private SerializedObject m_SettingsSO;

        private RenderTexture m_BakedSDF;
        private SdfBakerPreview m_MeshPreview;
        private Texture3DPreview m_TexturePreview;
        private bool m_RefreshMeshPreview = false;
        private bool m_ShowAdvanced;
        private MeshToSDFBaker m_Baker;
        private bool m_FoldOutParameters = true;
        private bool m_PrefabChanged = false;
        private bool m_LiveUpdate = false;
        private int maxResolution
        {
            get { return m_Settings.m_MaxResolution; }
            set { m_Settings.m_MaxResolution = value; }
        }
        private Vector3 boxSizeReference
        {
            get { return m_Settings.m_BoxSizeReference; }
            set { m_Settings.m_BoxSizeReference = value; }
        }
        private Vector3 boxCenter
        {
            get { return m_Settings.m_BoxCenter; }
            set { m_Settings.m_BoxCenter = value; }
        }
        private int signPassesCount
        {
            get { return m_Settings.m_SignPassesCount; }
            set { m_Settings.m_SignPassesCount = value; }
        }
        private float inOutThreshold
        {
            get { return m_Settings.m_InOutThreshold; }
            set { m_Settings.m_InOutThreshold = value; }
        }
        private float surfaceOffset
        {
            get { return m_Settings.m_SurfaceOffset; }
            set { m_Settings.m_SurfaceOffset = value; }
        }
        private ModelSource modelSource
        {
            get { return m_Settings.m_ModelSource; }
            set { m_Settings.m_ModelSource = value; }
        }
        private PreviewChoice previewObject
        {
            get { return m_Settings.m_PreviewObject; }
            set { m_Settings.m_PreviewObject = value; }
        }
        private Mesh selectedMesh
        {
            get { return m_Settings.m_SelectedMesh; }
            set { m_Settings.m_SelectedMesh = value; }
        }
        private GameObject meshPrefab
        {
            get { return m_Settings.m_MeshPrefab; }
            set { m_Settings.m_MeshPrefab = value; }
        }

        private Mesh mesh
        {
            get { return m_Settings.m_Mesh; }
            set { m_Settings.m_Mesh = value; }
        }

        [SerializeField]
        private Vector3 m_ActualBoxSize;


        protected void OnGUI()
        {
            if (m_Settings == null)
            {
                m_Settings = CreateInstance<SdfBakerSettings>();
                m_Settings.name = "None";
            }
            bool needsUpdate = false;
            Undo.RecordObject(this, "Settings Asset change");
            Undo.RecordObject(m_Settings, "SDF Baker Parameter change");
            GUILayout.BeginHorizontal();


            EditorGUI.BeginChangeCheck();
            var newSettings = (SdfBakerSettings)EditorGUILayout.ObjectField(Contents.settingsAsset, m_Settings, typeof(SdfBakerSettings), true, GUILayout.MinWidth(20),
                GUILayout.MaxWidth(400), GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                if (newSettings != null)
                {
                    LoadSettings(newSettings);
                }
                else
                {
                    CreateNewSession();
                }
            }
            GUILayout.Space(5);


            GUI.enabled = EditorUtility.IsDirty(m_Settings);
            if (GUILayout.Button(Contents.saveSettings, GUILayout.MinWidth(20), GUILayout.ExpandWidth(true)))
            {
                SaveSettings();
            }
            GUI.enabled = true;
            DrawContextIcon();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            maxResolution = Mathf.Clamp(EditorGUILayout.IntField(Contents.maxResolution, maxResolution), 8, 1024);
            bool maxResChanged = EditorGUI.EndChangeCheck();
            needsUpdate |= maxResChanged;
            if (maxResolution > 255)
                EditorGUILayout.HelpBox(
                    "Higher resolutions are more expensive to calculate and can make Unity unstable.",
                    MessageType.Warning);

            EditorGUI.BeginChangeCheck();
            var prevWideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            boxCenter = EditorGUILayout.Vector3Field("Box Center", boxCenter);
            needsUpdate |= EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            boxSizeReference = EditorGUILayout.Vector3Field(Contents.boxSizeReference, boxSizeReference);
            bool boxSizeChanged = EditorGUI.EndChangeCheck();
            needsUpdate |= boxSizeChanged;

            if (boxSizeChanged || maxResChanged)
            {
                m_ActualBoxSize = SnapBoxToVoxels();
            }

            using (new EditorGUI.DisabledScope(true))
            {
                SerializedObject selfSO = new SerializedObject(this);
                EditorGUILayout.PropertyField(selfSO.FindProperty("m_ActualBoxSize"), Contents.actualBoxSize);
            }


            if (boxSizeReference.x * boxSizeReference.y * boxSizeReference.z <= Single.Epsilon)
                EditorGUILayout.HelpBox("The volume of your bounding box is zero.", MessageType.Warning);

            int estimatedGridSize = EstimateGridSize();
            if (estimatedGridSize > 1 << 21 || selectedMesh == null)
            {
                GUI.enabled = false;
                m_LiveUpdate = false;
            }

            EditorGUI.BeginChangeCheck();
            m_LiveUpdate = EditorGUILayout.Toggle(Contents.liveUpdate, m_LiveUpdate);
            needsUpdate |= (EditorGUI.EndChangeCheck() & m_LiveUpdate);

            GUI.enabled = true;
            if (m_LiveUpdate)
                EditorGUILayout.HelpBox(
                    "Baking the mesh in real-time might cause slowdowns or instabilities when the resolution and/or the sign passes count is high ",
                    MessageType.Warning);

            bool fitPaddingChanged = false;
            if (m_ShowAdvanced)
            {
                m_FoldOutParameters = EditorGUILayout.BeginFoldoutHeaderGroup(m_FoldOutParameters, Contents.bakingParameters);
                EditorGUI.BeginChangeCheck();
                if (m_FoldOutParameters)
                {
                    EditorGUI.indentLevel++;
                    signPassesCount = Mathf.Clamp(EditorGUILayout.IntField(Contents.signPass, signPassesCount), 0,
                        20);
                    inOutThreshold = EditorGUILayout.Slider(Contents.inOutParam, inOutThreshold, 0.0f, 1.0f);
                    surfaceOffset = EditorGUILayout.Slider(Contents.sdfOffset, surfaceOffset, -0.5f, 0.5f);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                needsUpdate |= EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                m_Settings.m_FitPaddingVoxel = EditorGUILayout.Vector3IntField(Contents.fitPadding, m_Settings.m_FitPaddingVoxel);
                fitPaddingChanged = EditorGUI.EndChangeCheck();
            }
            EditorGUIUtility.wideMode = prevWideMode;

            if (mesh != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                if (GUILayout.Button(Contents.fitBoxToMesh))
                {
                    FitBoxToMesh();
                }
                needsUpdate |= EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                if (GUILayout.Button(Contents.fitCubeToMesh))
                {
                    FitCubeToMesh();
                }
                needsUpdate |= EditorGUI.EndChangeCheck();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.BeginChangeCheck();
            modelSource = (ModelSource)EditorGUILayout.EnumPopup(Contents.bakeSource, modelSource);
            bool changedSource = EditorGUI.EndChangeCheck();
            needsUpdate |= changedSource;

            switch (modelSource)
            {
                case ModelSource.Mesh:
                    EditorGUI.BeginChangeCheck();
                    selectedMesh =
                        (Mesh)EditorGUILayout.ObjectField(Contents.mesh, selectedMesh, typeof(Mesh), false);
                    bool meshFieldHasChanged = EditorGUI.EndChangeCheck();
                    needsUpdate |= meshFieldHasChanged;

                    m_RefreshMeshPreview |= meshFieldHasChanged | changedSource;
                    if (m_RefreshMeshPreview)
                    {
                        m_Settings.ApplySelectedMesh();
                    }

                    break;

                case ModelSource.MeshPrefab:
                    EditorGUI.BeginChangeCheck();
                    meshPrefab =
                        (GameObject)EditorGUILayout.ObjectField(Contents.meshPrefab, meshPrefab, typeof(GameObject),
                            false);

                    meshFieldHasChanged = EditorGUI.EndChangeCheck() || m_PrefabChanged;
                    m_PrefabChanged = false;
                    needsUpdate |= meshFieldHasChanged;

                    m_RefreshMeshPreview |= meshFieldHasChanged | changedSource || (mesh == null);
                    bool rebuildMesh = m_RefreshMeshPreview;
                    if (rebuildMesh)
                    {
                        m_Settings.BuildMeshFromPrefab();
                        rebuildMesh = false;
                    }

                    break;
            }

            if (mesh == null) m_LiveUpdate = false;

            if (m_RefreshMeshPreview && mesh != null)
            {
                FitBoxToMesh();
                m_MeshPreview?.Dispose();
                m_MeshPreview = new SdfBakerPreview(mesh);
                m_RefreshMeshPreview = false;
            }

            if (mesh == null || (estimatedGridSize > UnityEngine.VFX.SDF.MeshToSDFBaker.kMaxGridSize) || InternalMeshUtil.GetPrimitiveCount(mesh) == 0)
            {
                GUI.enabled = false;
            }

            if (GUILayout.Button("Bake mesh") || m_LiveUpdate && needsUpdate)
            {
                if (m_Baker == null)
                {
                    m_Baker = new MeshToSDFBaker(boxSizeReference, boxCenter, maxResolution, mesh, signPassesCount,
                        inOutThreshold, surfaceOffset);
                }
                else
                {
                    m_Baker.Reinit(boxSizeReference, boxCenter, maxResolution, mesh, signPassesCount,
                        inOutThreshold, surfaceOffset);
                }

                m_Baker.BakeSDF();
                m_BakedSDF = m_Baker.SdfTexture;
            }

            GUI.enabled = true;


            bool canSave = true;
            if ((m_BakedSDF == null) || (m_Baker == null))
            {
                canSave = false;
                GUI.enabled = false;
            }

            if (GUILayout.Button(canSave ? Contents.saveSDF : Contents.saveSDFBlocked))
            {
                m_Baker.SaveToAsset();
            }

            GUI.enabled = true;

            previewObject = (PreviewChoice)EditorGUILayout.EnumPopup(Contents.previewChoice, previewObject);
            if ((previewObject & PreviewChoice.Mesh) != 0)
            {
                UpdateMeshPreview();
            }

            if ((previewObject & PreviewChoice.Texture) != 0)
            {
                UpdateTexture3dPreview();
            }

            if (needsUpdate)
                EditorUtility.SetDirty(m_Settings);
        }

        private void UpdateTexture3dPreview()
        {
            if (m_BakedSDF)
            {
                if (m_TexturePreview == null) m_TexturePreview = CreateInstance<Texture3DPreview>();
                m_TexturePreview.Texture = m_BakedSDF;
                GUILayout.BeginHorizontal();
                m_TexturePreview.OnPreviewSettings(new Object[] { m_BakedSDF });
                GUILayout.EndHorizontal();
                Rect rect = GUILayoutUtility.GetRect(100, 2000, 100, 2000, GUIStyle.none);
                m_TexturePreview.OnPreviewGUI(rect, GUIStyle.none);
                EditorGUI.DropShadowLabel(rect, m_TexturePreview.GetInfoString());
            }
        }

        private void UpdateMeshPreview()
        {
            if (mesh)
            {
                GUILayout.BeginHorizontal(GUIContent.none, EditorStyles.toolbar);
                GUILayout.Label(mesh.name, new GUIStyle("ToolbarBoldLabel"));
                GUILayout.FlexibleSpace();
                m_MeshPreview?.OnPreviewSettings();
                GUILayout.EndHorizontal();
                m_MeshPreview.sizeBoxReference = boxSizeReference;
                m_MeshPreview.actualSizeBox = m_ActualBoxSize;
                m_MeshPreview.centerBox = boxCenter;
                m_MeshPreview?.OnPreviewGUI(GUILayoutUtility.GetRect(100, 2000, 100, 2000, GUIStyle.none),
                    GUIStyle.none);
            }
        }

        private void DrawContextIcon()
        {
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            var rect = GUILayoutUtility.GetRect(Contents.contextMenuIcon, GUIStyle.none, GUILayout.Height(20), GUILayout.Width(20));

            GUI.Button(rect, Contents.contextMenuIcon, GUIStyle.none);
            if (EditorGUI.EndChangeCheck())
            {
                OnContextClick(new Vector2(rect.x, rect.yMax));
            }
        }

        private void OnContextClick(Vector2 pos)
        {
            var menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("Show Additional Properties"), m_ShowAdvanced, () => m_ShowAdvanced = !m_ShowAdvanced);
            menu.AddItem(EditorGUIUtility.TrTextContent("Create new session"), false, CreateNewSession);
            menu.DropDown(new Rect(pos, Vector2.zero));
        }

        private void OnEnable()
        {
            titleContent = Contents.title;
            minSize = new Vector2(300.0f, 400.0f);
            if (m_Settings == null)
            {
                m_Settings = CreateInstance<SdfBakerSettings>();
                m_Settings.name = "None";
            }
            if (m_TexturePreview == null) m_TexturePreview = CreateInstance<Texture3DPreview>();
            m_TexturePreview.OnEnable();
            if (m_BakedSDF != null)
                m_TexturePreview.Texture = m_BakedSDF;
            if (m_MeshPreview == null) m_MeshPreview = new SdfBakerPreview(mesh); // Not sure if necessary
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;
            Selection.selectionChanged += OnSelectionChanged;
        }

        public void OnDisable()
        {
            m_MeshPreview?.Dispose();
            m_Baker?.Dispose();
            if (m_BakedSDF)
            {
                m_BakedSDF.Release();
            }

            if (m_TexturePreview)
            {
                m_TexturePreview.OnDisable();
                m_TexturePreview = null;
            }
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdated;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        void OnPrefabInstanceUpdated(GameObject prefab)
        {
            if (prefab != null && meshPrefab != null && prefab.name == meshPrefab.name)
            {
                m_PrefabChanged = true;
            }
        }

        void OnUndoRedoPerformed()
        {
            m_MeshPreview?.Dispose();
            m_MeshPreview = new SdfBakerPreview(mesh);
        }

        void OnSelectionChanged()
        {
            if (Selection.activeObject is SdfBakerSettings)
            {
                SdfBakerSettings settings = Selection.activeObject as SdfBakerSettings;
                LoadSettings(settings);
                Repaint();
            }
        }

        void FitBoxToMesh()
        {
            boxCenter = mesh.bounds.center;
            boxSizeReference = mesh.bounds.extents * 2.0f;
            var absolutePadding = GetAbsolutePadding();
            boxSizeReference += absolutePadding;
            m_ActualBoxSize = SnapBoxToVoxels();
            boxSizeReference = m_ActualBoxSize;
        }

        void FitCubeToMesh()
        {
            float maxSize = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.y, mesh.bounds.extents.z) * 2.0f;
            boxSizeReference = new Vector3(maxSize, maxSize, maxSize);
            boxCenter = mesh.bounds.center;
            var absolutePadding = GetAbsolutePadding();
            boxSizeReference += absolutePadding;
            m_ActualBoxSize = SnapBoxToVoxels();
        }

        private Vector3 GetAbsolutePadding()
        {
            float maxExtent = Mathf.Max(boxSizeReference.x, Mathf.Max(boxSizeReference.y, boxSizeReference.z));
            float voxelSize = maxExtent / maxResolution;
            Vector3 absolutePadding = 2 * voxelSize * new Vector3(m_Settings.m_FitPaddingVoxel.x, m_Settings.m_FitPaddingVoxel.y,
                m_Settings.m_FitPaddingVoxel.z);
            return absolutePadding;
        }

        int EstimateGridSize()
        {
            float maxExtent = Mathf.Max(boxSizeReference.x, Mathf.Max(boxSizeReference.y, boxSizeReference.z));
            int dimX, dimY, dimZ;

            if (maxExtent == boxSizeReference.x)
            {
                dimX = Mathf.Max(Mathf.RoundToInt(maxResolution * boxSizeReference.x / maxExtent), 1);
                dimY = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.y / maxExtent), 1);
                dimZ = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.z / maxExtent), 1);
            }
            else if (maxExtent == boxSizeReference.y)
            {
                dimY = Mathf.Max(Mathf.RoundToInt(maxResolution * boxSizeReference.y / maxExtent), 1);
                dimX = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.x / maxExtent), 1);
                dimZ = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.z / maxExtent), 1);
            }
            else
            {
                dimZ = Mathf.Max(Mathf.RoundToInt(maxResolution * boxSizeReference.z / maxExtent), 1);
                dimY = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.y / maxExtent), 1);
                dimX = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.x / maxExtent), 1);
            }

            return dimX * dimY * dimZ;
        }

        Vector3 SnapBoxToVoxels(int refAxis = 0)
        {
            float maxExtent = Mathf.Max(boxSizeReference.x, Mathf.Max(boxSizeReference.y, boxSizeReference.z));
            int dimX, dimY, dimZ;

            if (refAxis == 0 || refAxis > 3) // Default behavior, choose largest dimension
            {
                if (maxExtent == boxSizeReference.x)
                {
                    refAxis = 1;
                }

                if (maxExtent == boxSizeReference.y)
                {
                    refAxis = 2;
                }

                if (maxExtent == boxSizeReference.z)
                {
                    refAxis = 3;
                }
            }

            if (refAxis == 1)
            {
                dimX = Mathf.Max(Mathf.RoundToInt(maxResolution * boxSizeReference.x / maxExtent), 1);
                dimY = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.y / maxExtent), 1);
                dimZ = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.z / maxExtent), 1);
                float voxelSize = boxSizeReference.x / dimX;
                var tmpBoxSize = boxSizeReference;
                tmpBoxSize.x = dimX * voxelSize;
                tmpBoxSize.y = dimY * voxelSize;
                tmpBoxSize.z = dimZ * voxelSize;
                return tmpBoxSize;
            }
            else if (refAxis == 2)
            {
                dimY = Mathf.Max(Mathf.RoundToInt(maxResolution * boxSizeReference.y / maxExtent), 1);
                dimX = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.x / maxExtent), 1);
                dimZ = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.z / maxExtent), 1);
                float voxelSize = boxSizeReference.y / dimY;
                var tmpBoxSize = boxSizeReference;
                tmpBoxSize.x = dimX * voxelSize;
                tmpBoxSize.y = dimY * voxelSize;
                tmpBoxSize.z = dimZ * voxelSize;
                return tmpBoxSize;
            }
            else
            {
                dimZ = Mathf.Max(Mathf.RoundToInt(maxResolution * boxSizeReference.z / maxExtent), 1);
                dimY = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.y / maxExtent), 1);
                dimX = Mathf.Max(Mathf.CeilToInt(maxResolution * boxSizeReference.x / maxExtent), 1);
                float voxelSize = boxSizeReference.z / dimZ;
                var tmpBoxSize = boxSizeReference;
                tmpBoxSize.x = dimX * voxelSize;
                tmpBoxSize.y = dimY * voxelSize;
                tmpBoxSize.z = dimZ * voxelSize;
                return tmpBoxSize;
            }
        }

        void CreateNewSession()
        {
            if (AssetDatabase.Contains(m_Settings))
            {
                m_Settings = CreateInstance<SdfBakerSettings>();
                m_Settings.name = "None";
            }
            else
            {
                m_Settings.ResetToDefault();
            }
            m_BakedSDF = null;
            if (m_MeshPreview != null)
            {
                m_MeshPreview.Dispose();
                m_MeshPreview = null;
            }

            if (m_TexturePreview != null)
            {
                m_TexturePreview.OnDisable();
                m_TexturePreview = null;
            }

            m_RefreshMeshPreview = false;
            if (m_Baker != null)
            {
                m_Baker.Dispose();
                m_Baker = null;
            }
        }

        void SaveSettings()
        {
            string path;

            if (AssetDatabase.Contains(m_Settings))
            {
                bool restoreInspectorSelection = Selection.activeObject == m_Settings;
                path = AssetDatabase.GetAssetPath(m_Settings);
                var newSettings = Instantiate(m_Settings);
                AssetDatabase.DeleteAsset(path);
                newSettings.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(newSettings, path);
                m_Settings = newSettings;
                if (restoreInspectorSelection)
                {
                    Selection.activeObject = m_Settings;
                }
                return;
            }

            path = EditorUtility.SaveFilePanelInProject("Save the SDF Baker Settings as", "SDF Settings", "asset", "");
            if (path != "")
            {
                AssetDatabase.DeleteAsset(path);
                m_Settings.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(m_Settings, path);
            }
        }

        internal void LoadSettings(SdfBakerSettings newSettings)
        {
            m_Settings = newSettings;
            m_ActualBoxSize = SnapBoxToVoxels();
            if (m_TexturePreview != null)
            {
                m_TexturePreview.OnDisable();
            }
            m_TexturePreview = CreateInstance<Texture3DPreview>();
            if (m_MeshPreview != null)
            {
                m_MeshPreview.Dispose();
            }
            m_MeshPreview = new SdfBakerPreview(mesh);
            m_BakedSDF = null;
        }

        internal void ForceRefreshMeshPreview()
        {
            m_RefreshMeshPreview = true;
        }

        static class Contents
        {
            internal static GUIContent title = new GUIContent("SDF Bake Tool");
            internal static GUIContent maxResolution = new GUIContent("Max resolution", "Sets the number of voxels of the largest dimension.");
            internal static GUIContent mesh = new GUIContent("Mesh");
            internal static GUIContent boxSizeReference = new GUIContent("Desired Box Size", "Size of the desired Bounding Box. The Actual Bounding Box will be slightly modified to make sure that each voxel is a cube. Displayed in white in the mesh preview.");
            internal static GUIContent actualBoxSize = new GUIContent("Actual Box Size", "Size of the Bounding Box where the mesh will actually be baked. It may differ from the desired Bounding Box to make sure that each voxel is a cube. Displayed in green in the mesh preview.");
            internal static GUIContent boxCenter = new GUIContent("Box Center");
            internal static GUIContent meshPrefab = new GUIContent("Mesh Prefab");
            internal static GUIContent previewChoice = new GUIContent("Preview Object");
            internal static GUIContent bakeSource = new GUIContent("Model Source");
            internal static GUIContent liveUpdate = new GUIContent("Live Update", "When enabled, every modification to the settings will trigger a new bake of the SDF, and the preview will be updated accordingly.");

            internal static GUIContent signPass = new GUIContent("Sign passes count",
                "Increasing the number of sign passes can help refine the distinction between the inside and the outside of the mesh." +
                " This can be useful for models containing holes or self-intersections");

            internal static GUIContent inOutParam = new GUIContent("In/Out Threshold",
                "This helps arbitrate what is inside from what is outside of the shape. Low values will include more points in the inside (negative sign), and vice versa.");
            internal static GUIContent sdfOffset = new GUIContent("Surface Offset",
                "Selects the amount of offset applied to the surface. This amount is in the normalized voxel space.");

            internal static GUIContent saveSDF = new GUIContent("Save SDF");

            internal static GUIContent saveSDFBlocked = new GUIContent("Save SDF",
                "There is nothing to save yet. Please use the Bake Mesh button before saving.");
            static Texture2D paneOptionsIconDark = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");
            static Texture2D paneOptionsIconLight = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png");
            static Texture2D paneOptionsIcon { get { return EditorGUIUtility.isProSkin ? paneOptionsIconDark : paneOptionsIconLight; } }
            internal static GUIContent contextMenuIcon = new GUIContent(paneOptionsIcon, "Additional Properties");

            internal static GUIContent fitPadding = new GUIContent("Fit Padding", "Controls the padding, in voxel, to apply when using \"Fit Box/Cube to Mesh\".");
            internal static GUIContent fitBoxToMesh = new GUIContent("Fit box to Mesh", "Fits the bounding box of the bake to the bounding box of the mesh. Padding specified in \"Fit Padding\" (in Additional Properties) will be applied.");
            internal static GUIContent fitCubeToMesh = new GUIContent("Fit cube to Mesh", "Fits the bounding box of the bake to the bounding cube of the mesh. Padding specified in \"Fit Padding\" (in Additional Properties) will be applied.");
            internal static GUIContent bakingParameters = new GUIContent("Baking parameters");
            internal static GUIContent createNewSession = new GUIContent("New Session", "Resets the tool to its default parameters, creating a new unsaved settings assets. This will also erase the current baked SDF texture if there is any.");
            internal static GUIContent saveSettings = new GUIContent("Save Settings", "Saves the settings of the tool into an asset.");
            internal static GUIContent settingsAsset = new GUIContent("Settings Asset");
        }
    }
}
