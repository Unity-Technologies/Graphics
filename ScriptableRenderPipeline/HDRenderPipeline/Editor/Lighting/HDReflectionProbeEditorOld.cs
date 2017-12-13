using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.Experimental.Rendering;

namespace UnityEditor
{
    //[CustomEditorForRenderPipeline(typeof(ReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    public class HDReflectionProbeEditorOld : Editor
    {
        static HDReflectionProbeEditorOld s_LastInteractedEditorOld;

        SerializedProperty m_Mode;
        SerializedProperty m_RefreshMode;
        SerializedProperty m_TimeSlicingMode;
        SerializedProperty m_Resolution;
        SerializedProperty m_ShadowDistance;
        SerializedProperty m_BoxSize;
        SerializedProperty m_BoxOffset;
        SerializedProperty m_CullingMask;
        SerializedProperty m_ClearFlags;
        SerializedProperty m_BackgroundColor;
        SerializedProperty m_HDR;
        SerializedProperty m_BoxProjection;
        SerializedProperty m_IntensityMultiplier;
        SerializedProperty m_BlendDistance;
        SerializedProperty m_CustomBakedTexture;
        SerializedProperty m_RenderDynamicObjects;
        SerializedProperty m_UseOcclusionCulling;
        SerializedProperty m_BakedTexture;

        SerializedProperty[] m_NearAndFarProperties;

        SerializedObject additionalReflectionDataSerializedObject;

        SerializedProperty m_InfluenceShape;
        SerializedProperty m_InfluenceSphereRadius;
        SerializedProperty m_Dimmer;
        SerializedProperty m_UseSeparateProjectionVolume;
        SerializedProperty m_BoxReprojectionVolumeSize;
        SerializedProperty m_BoxReprojectionVolumeCenter;
        SerializedProperty m_SphereReprojectionVolumeRadius;
        SerializedProperty m_PreviewCubemap;
        SerializedProperty m_MaxSearchDistance;

        private static Mesh s_SphereMesh;
        private Material m_ReflectiveMaterial;
        private Matrix4x4 m_OldLocalSpace = Matrix4x4.identity;
        private float m_Exposure = 0.0F;
        private float m_MipLevelPreview = 0.0F;
        private string m_TargetPath;

        public bool drawFitFunctionDebug = false;
        public int rayCount = 128;

        private BoxBoundsHandle m_BoxInfluenceBoundsHandle = new BoxBoundsHandle();
        private BoxBoundsHandle m_BoxProjectionBoundsHandle = new BoxBoundsHandle();
        private BoxBoundsHandle m_BoxBlendHandle = new BoxBoundsHandle();
        private SphereBoundsHandle m_InfluenceSphereHandle = new SphereBoundsHandle();
        private SphereBoundsHandle m_ProjectionSphereHandle = new SphereBoundsHandle();
        private SphereBoundsHandle m_SphereBlendHandle = new SphereBoundsHandle();

        private Hashtable m_CachedGizmoMaterials = new Hashtable();

        static internal class Styles
        {
            static Styles()
            {
                richTextMiniLabel.richText = true;

                // Create a list of cubemap resolutions
                renderTextureSizesValues.Clear();
                renderTextureSizes.Clear();

                int cubemapResolution = ReflectionProbe.minBakedCubemapResolution;

                do
                {
                    renderTextureSizesValues.Add(cubemapResolution);
                    renderTextureSizes.Add(new GUIContent(cubemapResolution.ToString()));
                    cubemapResolution *= 2;
                }
                while (cubemapResolution <= ReflectionProbe.maxBakedCubemapResolution);
            }

            public static GUIStyle richTextMiniLabel = new GUIStyle(EditorStyles.miniLabel);

            // HDRP begin
            public static GUIContent bakeButtonText = new GUIContent("Bake");
            // HDRP end
            public static string[] bakeCustomOptionText = { "Bake as new Cubemap..." };
            public static string[] bakeButtonsText = { "Bake All Reflection Probes" };

            public static GUIContent bakeCustomButtonText = new GUIContent("Bake", "Bakes Reflection Probe's cubemap, overwriting the existing cubemap texture asset (if any).");
            public static GUIContent runtimeSettingsHeader = new GUIContent("Runtime settings", "These settings are used by objects when they render with the cubemap of this probe");
            public static GUIContent backgroundColorText = new GUIContent("Background", "Camera clears the screen to this color before rendering.");
            public static GUIContent clearFlagsText = new GUIContent("Clear Flags");
            public static GUIContent intensityText = new GUIContent("Intensity");
            public static GUIContent resolutionText = new GUIContent("Resolution");
            public static GUIContent captureCubemapHeaderText = new GUIContent("Cubemap capture settings");
            public static GUIContent boxProjectionText = new GUIContent("Box Projection", "Box projection causes reflections to appear to change based on the object's position within the probe's box, while still using a single probe as the source of the reflection. This works well for reflections on objects that are moving through enclosed spaces such as corridors and rooms. Setting box projection to False and the cubemap reflection will be treated as coming from infinitely far away. Note that this feature can be globally disabled from Graphics Settings -> Tier Settings");
            public static GUIContent blendDistanceText = new GUIContent("Blend Distance", "Area around the probe where it is blended with other probes. Only used in deferred probes.");
            public static GUIContent sizeText = new GUIContent("Box Size", "The size of the box in which the reflections will be applied to objects. The value is not affected by the Transform of the Game Object.");
            public static GUIContent centerText = new GUIContent("Box Offset", "The center of the box in which the reflections will be applied to objects. The value is relative to the position of the Game Object.");
            public static GUIContent customCubemapText = new GUIContent("Cubemap");
            public static GUIContent renderDynamicObjects = new GUIContent("Dynamic Objects", "If enabled dynamic objects are also rendered into the cubemap");
            public static GUIContent timeSlicing = new GUIContent("Time Slicing", "If enabled this probe will update over several frames, to help reduce the impact on the frame rate");
            public static GUIContent refreshMode = new GUIContent("Refresh Mode", "Controls how this probe refreshes in the Player");

            public static GUIContent typeText = new GUIContent("Type", "'Baked Cubemap' uses the 'Auto Baking' mode from the Lighting window. If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n'Custom' can be used if a custom cubemap is wanted. \n'Realtime' can be used to dynamically re-render the cubemap during runtime (via scripting).");
            public static GUIContent[] reflectionProbeMode = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
            public static int[] reflectionProbeModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Custom, (int)ReflectionProbeMode.Realtime };

            public static GUIContent shape = new GUIContent("Shape", "Controls the shape of the reflection probe volume.");

            public static List<int> renderTextureSizesValues = new List<int>();
            public static List<GUIContent> renderTextureSizes = new List<GUIContent>();

            // HDRP begin
            public static GUIContent[] toolContents =
            {
                EditorGUIUtility.IconContent("d_EditCollider", "|Modify the influence volume of the reflection probe."),
                EditorGUIUtility.IconContent("d_PreMatCube", "|Modify the projection volume of the reflection probe."),
                EditorGUIUtility.IconContent("d_Navigation", "|Fit the reflection probe volume to the surrounding colliders."),
                EditorGUIUtility.IconContent("MoveTool", "|Move the selected objects.")
            };
            // HDRP end

            public static EditMode.SceneViewEditMode[] sceneViewEditModes = new[]
            {
                EditMode.SceneViewEditMode.ReflectionProbeBox,
                EditMode.SceneViewEditMode.GridBox,
                EditMode.SceneViewEditMode.Collider,
                EditMode.SceneViewEditMode.ReflectionProbeOrigin
            };

            public static string baseSceneEditingToolText = "<color=grey>Probe Scene Editing Mode:</color> \n";
            public static GUIContent[] toolNames =
            {
                new GUIContent(baseSceneEditingToolText + "Box Influence Bounds", ""),
                new GUIContent(baseSceneEditingToolText + "Box Projection Bounds", ""),
                new GUIContent(baseSceneEditingToolText + "Fit Projection Volume", ""),
                new GUIContent(baseSceneEditingToolText + "Probe Origin", "")
            };
        } // end of class Styles

        // Should match reflection probe gizmo color in GizmoDrawers.cpp!
        internal static Color kGizmoReflectionProbe = new Color(0xFF / 255f, 0xE5 / 255f, 0x94 / 255f, 0x80 / 255f);
        internal static Color kGizmoReflectionProbeDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x60 / 255f);
        internal static Color kGizmoHandleReflectionProbe = new Color(0xFF / 255f, 0xE5 / 255f, 0xAA / 255f, 0xFF / 255f);

        readonly AnimBool m_ShowProbeModeRealtimeOptions = new AnimBool(); // p.mode == ReflectionProbeMode.Realtime; Will be brought back in 5.1
        readonly AnimBool m_ShowProbeModeCustomOptions = new AnimBool();
        readonly AnimBool m_ShowBoxOptions = new AnimBool();

        private HDCubemapInspector m_CubemapEditor = null;

        bool IsReflectionProbeEditMode(EditMode.SceneViewEditMode editMode)
        {
            return editMode == EditMode.SceneViewEditMode.ReflectionProbeBox || editMode == EditMode.SceneViewEditMode.Collider || editMode == EditMode.SceneViewEditMode.GridBox ||
                editMode == EditMode.SceneViewEditMode.ReflectionProbeOrigin;
        }

        bool sceneViewEditing
        {
            get { return IsReflectionProbeEditMode(EditMode.editMode) && EditMode.IsOwner(this); }
        }

        public void OnEnable()
        {
            m_Mode = serializedObject.FindProperty("m_Mode");
            m_RefreshMode = serializedObject.FindProperty("m_RefreshMode");
            m_TimeSlicingMode = serializedObject.FindProperty("m_TimeSlicingMode");

            m_Resolution = serializedObject.FindProperty("m_Resolution");
            m_NearAndFarProperties = new[] { serializedObject.FindProperty("m_NearClip"), serializedObject.FindProperty("m_FarClip") };
            m_ShadowDistance = serializedObject.FindProperty("m_ShadowDistance");
            m_BoxSize = serializedObject.FindProperty("m_BoxSize");
            m_BoxOffset = serializedObject.FindProperty("m_BoxOffset");
            m_CullingMask = serializedObject.FindProperty("m_CullingMask");
            m_ClearFlags = serializedObject.FindProperty("m_ClearFlags");
            m_BackgroundColor = serializedObject.FindProperty("m_BackGroundColor");
            m_HDR = serializedObject.FindProperty("m_HDR");
            m_BoxProjection = serializedObject.FindProperty("m_BoxProjection");
            m_IntensityMultiplier = serializedObject.FindProperty("m_IntensityMultiplier");
            m_BlendDistance = serializedObject.FindProperty("m_BlendDistance");
            m_CustomBakedTexture = serializedObject.FindProperty("m_CustomBakedTexture");
            m_BakedTexture = serializedObject.FindProperty("m_BakedTexture");
            m_RenderDynamicObjects = serializedObject.FindProperty("m_RenderDynamicObjects");
            m_UseOcclusionCulling = serializedObject.FindProperty("m_UseOcclusionCulling");

            ReflectionProbe p = target as ReflectionProbe;

            var additionalReflectionDatas = this.targets.Select(t => (t as Component).GetComponent<HDAdditionalReflectionData>()).ToArray();

            for (int i = 0; i < additionalReflectionDatas.Length; ++i)
            {
                if (additionalReflectionDatas[i] == null)
                {
                    additionalReflectionDatas[i] = Undo.AddComponent<HDAdditionalReflectionData>((targets[i] as Component).gameObject);
                }
            }

            additionalReflectionDataSerializedObject = new SerializedObject(additionalReflectionDatas);

            m_InfluenceShape = additionalReflectionDataSerializedObject.FindProperty("m_InfluenceShape");
            m_InfluenceSphereRadius = additionalReflectionDataSerializedObject.FindProperty("m_InfluenceSphereRadius");
            m_Dimmer = additionalReflectionDataSerializedObject.FindProperty("m_Dimmer");
            m_UseSeparateProjectionVolume = additionalReflectionDataSerializedObject.FindProperty("m_UseSeparateProjectionVolume");
            m_BoxReprojectionVolumeSize = additionalReflectionDataSerializedObject.FindProperty("m_BoxReprojectionVolumeSize");
            m_BoxReprojectionVolumeCenter = additionalReflectionDataSerializedObject.FindProperty("m_BoxReprojectionVolumeCenter");
            m_SphereReprojectionVolumeRadius = additionalReflectionDataSerializedObject.FindProperty("m_SphereReprojectionVolumeRadius");
            m_PreviewCubemap = additionalReflectionDataSerializedObject.FindProperty("m_PreviewCubemap");
            m_ShowProbeModeRealtimeOptions.valueChanged.AddListener(Repaint);
            m_ShowProbeModeCustomOptions.valueChanged.AddListener(Repaint);
            m_ShowBoxOptions.valueChanged.AddListener(Repaint);
            m_ShowProbeModeRealtimeOptions.value = p.mode == ReflectionProbeMode.Realtime;
            m_ShowProbeModeCustomOptions.value = p.mode == ReflectionProbeMode.Custom;
            m_ShowBoxOptions.value = true;
            m_MaxSearchDistance = additionalReflectionDataSerializedObject.FindProperty("m_MaxSearchDistance");

            m_BoxInfluenceBoundsHandle.handleColor = kGizmoHandleReflectionProbe;
            m_BoxInfluenceBoundsHandle.wireframeColor = Color.clear;

            RefreshPreviewSphere();

            UpdateOldLocalSpace();
        }

        public void OnDisable()
        {

            DestroyImmediate(m_CubemapEditor);
            DestroyImmediate(m_ReflectiveMaterial);

            foreach (Material mat in m_CachedGizmoMaterials.Values)
                DestroyImmediate(mat);
            m_CachedGizmoMaterials.Clear();
            ((HDAdditionalReflectionData)additionalReflectionDataSerializedObject.targetObject).ChangeVisibility(false);
        }

        private bool IsCollidingWithOtherProbes(string targetPath, ReflectionProbe targetProbe, out ReflectionProbe collidingProbe)
        {
            ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>().ToArray();
            collidingProbe = null;
            foreach (var probe in probes)
            {
                if (probe == targetProbe || probe.customBakedTexture == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(probe.customBakedTexture);
                if (path == targetPath)
                {
                    collidingProbe = probe;
                    return true;
                }
            }
            return false;
        }

        private void BakeReflectionProbe(ReflectionProbe probe, bool usePreviousAssetPath, bool custom)
        {
            if (custom == false && probe.bakedTexture != null)
                probe.customBakedTexture = probe.bakedTexture;

            string path = "";
            if (usePreviousAssetPath)
                path = AssetDatabase.GetAssetPath(probe.customBakedTexture);

            string targetExtension = probe.hdr ? "exr" : "png";
            if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != "." + targetExtension)
            {
                // We use the path of the active scene as the target path
                // HDRP begin
                //string targetPath = FileUtil.GetPathWithoutExtension(SceneManager.GetActiveScene().path);
                string targetPath = SceneManager.GetActiveScene().path.Remove(SceneManager.GetActiveScene().path.Length - 6);
                m_TargetPath = targetPath;
                // HDRP end
                if (string.IsNullOrEmpty(targetPath))
                    targetPath = "Assets";
                else if (Directory.Exists(targetPath) == false)
                    Directory.CreateDirectory(targetPath);

                string fileName = probe.name + (probe.hdr ? "-reflectionHDR" : "-reflection") + "." + targetExtension;
                fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetPath, fileName)));

                path = EditorUtility.SaveFilePanelInProject("Save reflection probe's cubemap.", fileName, targetExtension, "", targetPath);
                if (string.IsNullOrEmpty(path))
                    return;

                ReflectionProbe collidingProbe;
                if (IsCollidingWithOtherProbes(path, probe, out collidingProbe))
                {
                    if (!EditorUtility.DisplayDialog("Cubemap is used by other reflection probe",
                            string.Format("'{0}' path is used by the game object '{1}', do you really want to overwrite it?",
                                path, collidingProbe.name), "Yes", "No"))
                    {
                        return;
                    }
                }
            }

            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
            if (!Lightmapping.BakeReflectionProbe(probe, path))
                Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();
            RefreshPreviewSphere();
        }

        public void RefreshPreviewSphere()
        {
            if (reflectionProbeTarget != null)
                m_PreviewCubemap.objectReferenceValue = reflectionProbeTarget.texture;
            additionalReflectionDataSerializedObject.ApplyModifiedProperties();
        }

        private void OnBakeCustomButton(object data)
        {
            int mode = (int)data;

            ReflectionProbe p = target as ReflectionProbe;
            if (mode == 0)
                BakeReflectionProbe(p, false, true);
        }

        private void OnBakeButton(object data)
        {
            int mode = (int)data;
            //if (mode == 0)
            // HDRP Lightmapping.BakeAllReflectionProbesSnapshots();
        }

        ReflectionProbe reflectionProbeTarget
        {
            get { return (ReflectionProbe)target; }
        }

        void DoBakeButton()
        {
            if (reflectionProbeTarget.mode == ReflectionProbeMode.Realtime)
            {
                EditorGUILayout.HelpBox("Baking of this reflection probe should be initiated from the scripting API because the type is 'Realtime'", MessageType.Info);

                if (!QualitySettings.realtimeReflectionProbes)
                    EditorGUILayout.HelpBox("Realtime reflection probes are disabled in Quality Settings", MessageType.Warning);
                return;
            }

            if (reflectionProbeTarget.mode == ReflectionProbeMode.Baked && Lightmapping.giWorkflowMode != Lightmapping.GIWorkflowMode.OnDemand)
            {
                EditorGUILayout.HelpBox("Baking of this reflection probe is automatic because this probe's type is 'Baked' and the Lighting window is using 'Auto Baking'. The cubemap created is stored in the GI cache.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            switch (reflectionProbeMode)
            {
                case ReflectionProbeMode.Custom:

                    if (GUILayout.Button(new GUIContent("Bake"), GUILayout.MaxWidth(150)))
                    {
                        BakeReflectionProbe(reflectionProbeTarget, true, true);
                        GUIUtility.ExitGUI();
                    }
                    if (GUILayout.Button(new GUIContent("Bake as new cubemap")))
                    {
                        BakeReflectionProbe(reflectionProbeTarget, false, true);
                        GUIUtility.ExitGUI();
                    }

                    break;

                case ReflectionProbeMode.Baked:
                    using (new EditorGUI.DisabledScope(!reflectionProbeTarget.enabled))
                    {
                        // HDRP  Bake button in non-continous mode
                        // HDRP if (EditorGUI.ButtonWithDropdownList(Styles.bakeButtonText, Styles.bakeButtonsText, OnBakeButton))
                        // HDRP if (GUILayout.Button(new GUIContent("Bake"))) OnBakeButton(target);
                        if (GUILayout.Button(new GUIContent("Bake"), GUILayout.MaxWidth(150)))
                        {
                            BakeReflectionProbe(reflectionProbeTarget, true, false);
                            GUIUtility.ExitGUI();
                        }
                    }
                    break;

                case ReflectionProbeMode.Realtime:
                    // Not showing bake button in realtime
                    break;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void ToolbarGUI()
        {
            if (targets.Length > 1)
                return;
            // Show the master tool selector
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;
            var oldEditMode = EditMode.editMode;

            EditorGUI.BeginChangeCheck();
            EditMode.DoInspectorToolbar(Styles.sceneViewEditModes, Styles.toolContents, new Bounds(), this);
            if (EditorGUI.EndChangeCheck())
                s_LastInteractedEditorOld = this;

            if (oldEditMode != EditMode.editMode)
            {
                switch (EditMode.editMode)
                {
                    case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                        UpdateOldLocalSpace();
                        break;
                }
                // HDRP if (Toolbar.get != null)
                // HDRP  Toolbar.get.Repaint();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Info box for tools
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            string helpText = Styles.baseSceneEditingToolText;
            if (sceneViewEditing)
            {
                int index = ArrayUtility.IndexOf(Styles.sceneViewEditModes, EditMode.editMode);
                if (index >= 0)
                    helpText = Styles.toolNames[index].text;
            }
            GUILayout.Label(helpText, Styles.richTextMiniLabel);
            GUILayout.EndVertical();
            GUILayout.Space(EditorGUIUtility.fieldWidth);
            GUILayout.EndHorizontal();
        }

        ReflectionProbeMode reflectionProbeMode
        {
            get { return reflectionProbeTarget.mode; }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            additionalReflectionDataSerializedObject.Update();

            m_ShowProbeModeRealtimeOptions.target = reflectionProbeMode == ReflectionProbeMode.Realtime;
            m_ShowProbeModeCustomOptions.target = reflectionProbeMode == ReflectionProbeMode.Custom;

            EditorGUILayout.Space();

            SettingsGUI();

            EditorGUILayout.Space();

            InfluenceVolumeGUI();

            ProjectionVolumeGUI();

            EditorGUILayout.Space();

            CaptureSettingsGUI();

            AdditionalSettingsGUI();

            EditorGUILayout.Space();

            DoBakeButton();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Toggle Additional reflection data visibility"), GUILayout.MaxWidth(250)))
            {
                ToggleAdditionalComponentsVisibility();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
            additionalReflectionDataSerializedObject.ApplyModifiedProperties();
        }

        private void SettingsGUI()
        {
            //Settings
            //EditorLightingUtilities.DrawSplitter();
            //m_Mode.isExpanded = EditorLightingUtilities.DrawHeaderFoldout("Settings", m_Mode.isExpanded);

            EditorGUI.indentLevel++;
            //if (m_Mode.isExpanded)
            //{
                //Shape
                EditorGUILayout.IntPopup(m_Mode, Styles.reflectionProbeMode, Styles.reflectionProbeModeValues, Styles.typeText);

                // We cannot show multiple different type controls
                if (!m_Mode.hasMultipleDifferentValues)
                {
                    EditorGUI.indentLevel++;
                    {
                        // Custom cubemap UI (Bake button and manual cubemap assignment)
                        if (EditorGUILayout.BeginFadeGroup(m_ShowProbeModeCustomOptions.faded))
                        {
                            EditorGUILayout.PropertyField(m_RenderDynamicObjects, Styles.renderDynamicObjects);

                            EditorGUI.BeginChangeCheck();
                            EditorGUI.showMixedValue = m_CustomBakedTexture.hasMultipleDifferentValues;
                            var newCubemap = EditorGUILayout.ObjectField(Styles.customCubemapText, m_CustomBakedTexture.objectReferenceValue, typeof(Cubemap), false);
                            EditorGUI.showMixedValue = false;
                            if (EditorGUI.EndChangeCheck())
                                m_CustomBakedTexture.objectReferenceValue = newCubemap;
                        }
                        EditorGUILayout.EndFadeGroup();

                        // Realtime UI
                        if (EditorGUILayout.BeginFadeGroup(m_ShowProbeModeRealtimeOptions.faded))
                        {
                            EditorGUILayout.PropertyField(m_RefreshMode, Styles.refreshMode);
                            EditorGUILayout.PropertyField(m_TimeSlicingMode, Styles.timeSlicing);

                            EditorGUILayout.Space();
                        }
                        EditorGUILayout.EndFadeGroup();
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(m_InfluenceShape, new GUIContent("Shape"));
                EditorGUILayout.PropertyField(m_IntensityMultiplier, Styles.intensityText);
                EditorGUILayout.Space();
                ToolbarGUI();

            //}
            EditorGUI.indentLevel--;
        }

        private void InfluenceVolumeGUI()
        {
            CoreEditorUtils.DrawSplitter();
            m_BlendDistance.isExpanded = CoreEditorUtils.DrawHeaderFoldout("Influence volume settings", m_BlendDistance.isExpanded);

            EditorGUI.indentLevel++;

            if (m_BlendDistance.isExpanded)
            {
                EditorGUILayout.PropertyField(m_BlendDistance, Styles.blendDistanceText);

                //Box shape
                if (m_InfluenceShape.enumValueIndex == 0)
                {
                    m_BoxProjection.boolValue = true;

                    if (EditorGUILayout.BeginFadeGroup(m_ShowBoxOptions.faded))
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(m_BoxSize, Styles.sizeText);
                        EditorGUILayout.PropertyField(m_BoxOffset, Styles.centerText);
                        
                        if (EditorGUI.EndChangeCheck())
                        {
                            Vector3 center = m_BoxOffset.vector3Value;
                            Vector3 size = m_BoxSize.vector3Value;
                            if (ValidateAABB(ref center, ref size))
                            {
                                m_BoxOffset.vector3Value = center;
                                m_BoxSize.vector3Value = size;
                            }
                        }
                        
                    }
                    EditorGUILayout.EndFadeGroup();
                }
                //Sphere shape
                if (m_InfluenceShape.enumValueIndex == 1)
                {
                    EditorGUILayout.PropertyField(m_InfluenceSphereRadius, new GUIContent("Radius"));
                }
                EditorGUILayout.PropertyField(m_UseSeparateProjectionVolume);
            }
            EditorGUI.indentLevel--;
        }

        private void ProjectionVolumeGUI()
        {
            if (m_UseSeparateProjectionVolume.boolValue)
            {
                EditorGUILayout.Space();

                CoreEditorUtils.DrawSplitter();
                m_UseSeparateProjectionVolume.isExpanded = CoreEditorUtils.DrawHeaderFoldout("Reprojection volume settings", m_UseSeparateProjectionVolume.isExpanded);

                EditorGUI.indentLevel++;
                if (m_UseSeparateProjectionVolume.isExpanded)
                {
                    if (m_InfluenceShape.enumValueIndex == 0)
                    {
                        EditorGUILayout.PropertyField(m_BoxReprojectionVolumeSize);
                        EditorGUILayout.PropertyField(m_BoxReprojectionVolumeCenter);
                    }
                    if (m_InfluenceShape.enumValueIndex == 1)
                    {
                        EditorGUILayout.PropertyField(m_SphereReprojectionVolumeRadius);
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void CaptureSettingsGUI()
        {
            CoreEditorUtils.DrawSplitter();
            m_Resolution.isExpanded = CoreEditorUtils.DrawHeaderFoldout("Capture settings", m_Resolution.isExpanded);

            EditorGUI.indentLevel++;
            if (m_Resolution.isExpanded)
            {
                EditorGUILayout.IntPopup(m_Resolution, Styles.renderTextureSizes.ToArray(), Styles.renderTextureSizesValues.ToArray(), Styles.resolutionText, GUILayout.MinWidth(40));

                HDRenderPipelineAsset renderPipelineAsset = (HDRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;

                if (m_Resolution.intValue != renderPipelineAsset.globalTextureSettings.reflectionCubemapSize)
                {
                    EditorGUILayout.HelpBox("The resolution you chose is not the standard resolution used by your render pipeline. This reflection probe will not be used for the scene reflections.", MessageType.Warning);
                }

                EditorGUILayout.PropertyField(m_ShadowDistance);
                EditorGUILayout.PropertyField(m_CullingMask);
                EditorGUILayout.PropertyField(m_UseOcclusionCulling);
                foreach (SerializedProperty property in m_NearAndFarProperties)
                {
                    EditorGUILayout.PropertyField(property);
                }
            }
            EditorGUI.indentLevel--;
        }

        private void AdditionalSettingsGUI()
        {
            CoreEditorUtils.DrawSplitter();
            m_MaxSearchDistance.isExpanded = CoreEditorUtils.DrawHeaderFoldout("Additional settings", m_MaxSearchDistance.isExpanded);

            EditorGUI.indentLevel++;
            if (m_MaxSearchDistance.isExpanded)
            {
                EditorGUILayout.PropertyField(m_Dimmer);
                EditorGUILayout.PropertyField(m_MaxSearchDistance);
                drawFitFunctionDebug = EditorGUILayout.Toggle("Draw fit function debug rays", drawFitFunctionDebug);
                rayCount = EditorGUILayout.IntField("Fit function ray count", rayCount);
            }
            EditorGUI.indentLevel--;

            if (targets.Length == 1)
            {
                ReflectionProbe probe = reflectionProbeTarget;
                if (probe.mode == ReflectionProbeMode.Custom && probe.customBakedTexture != null)
                {
                    Cubemap cubemap = probe.customBakedTexture as Cubemap;
                    if (cubemap && cubemap.mipmapCount == 1)
                        EditorGUILayout.HelpBox("No mipmaps in the cubemap, Smoothness value in Standard shader will be ignored.", MessageType.Warning);
                }
            }
        }

        private void ToggleAdditionalComponentsVisibility()
        {
            var visible = new bool();
            var meshRenderer = reflectionProbeTarget.GetComponent<MeshRenderer>();
            var meshFilter = reflectionProbeTarget.GetComponent<MeshFilter>();
            visible = reflectionProbeTarget.GetComponent<HDAdditionalReflectionData>().hideFlags == HideFlags.None ? true : false;

            reflectionProbeTarget.GetComponent<HDAdditionalReflectionData>().hideFlags = visible ? HideFlags.HideInInspector : HideFlags.None;
            meshRenderer.hideFlags = visible ? HideFlags.HideInInspector : HideFlags.None;
            meshFilter.hideFlags = visible ? HideFlags.HideInInspector : HideFlags.None;
        }

        internal Bounds GetWorldBoundsOfTarget(Object targetObject)
        {
            return ((ReflectionProbe)targetObject).bounds;
        }

        bool ValidPreviewSetup()
        {
            ReflectionProbe p = reflectionProbeTarget;
            return (p != null && p.texture != null);
        }

        public override bool HasPreviewGUI()
        {
            if (targets.Length > 1)
                return false;  // We only handle one preview for reflection probes

            // Ensure valid cube map editor (if possible)
            if (ValidPreviewSetup())
            {
                Editor editor = m_CubemapEditor;
                Editor.CreateCachedEditor(((ReflectionProbe)target).texture, null, ref editor);
                m_CubemapEditor = editor as HDCubemapInspector;
            }

            // If having one probe selected we always want preview (to prevent preview window from popping)
            return true;
        }

        public override void OnPreviewSettings()
        {
            if (!ValidPreviewSetup())
                return;

            m_CubemapEditor.m_PreviewExposure = m_Exposure;
            m_CubemapEditor.m_MipLevelPreview = m_MipLevelPreview;

            EditorGUI.BeginChangeCheck();
            m_CubemapEditor.OnPreviewSettings();
            // Need to repaint, because mipmap value changes affect reflection probe preview in the scene
            if (EditorGUI.EndChangeCheck())
            {
                //EditorApplication.SetSceneRepaintDirty();
                m_Exposure = m_CubemapEditor.m_PreviewExposure;
                m_MipLevelPreview = m_CubemapEditor.m_MipLevelPreview;
            }
        }

        public override void OnPreviewGUI(Rect position, GUIStyle style)
        {
            if (!ValidPreviewSetup())
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Color prevColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.5f);
                GUILayout.Label("Reflection Probe not baked yet");
                GUI.color = prevColor;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }

            ReflectionProbe p = reflectionProbeTarget;
            if (p != null && p.texture != null && targets.Length == 1)
            {
                m_CubemapEditor.DrawPreview(position);
            }

        }

        private static Mesh sphereMesh
        {
            get { return s_SphereMesh ?? (s_SphereMesh = Resources.GetBuiltinResource(typeof(Mesh), "New-Sphere.fbx") as Mesh); }
        }

        private Material reflectiveMaterial
        {
            get
            {
                if (m_ReflectiveMaterial == null)
                {
                    m_ReflectiveMaterial = (Material)Instantiate(AssetDatabase.LoadAssetAtPath("Assets/ScriptableRenderPipeline/HDRenderPipeline/Debug/PreviewCubemapMaterial.mat", typeof(Material)));
                    m_ReflectiveMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_ReflectiveMaterial;
            }
        }

        private float GetProbeIntensity(ReflectionProbe p)
        {
            if (p == null || p.texture == null)
                return 1.0f;
            return p.intensity;
        }

        // Ensures that probe's AABB encapsulates probe's position
        // Returns true, if center or size was modified
        private bool ValidateAABB(ref Vector3 center, ref Vector3 size)
        {
            ReflectionProbe p = reflectionProbeTarget;

            Matrix4x4 localSpace = GetLocalSpace(p);
            Vector3 localTransformPosition = localSpace.inverse.MultiplyPoint3x4(p.transform.position);

            Bounds b = new Bounds(center, size);

            if (b.Contains(localTransformPosition)) return false;

            b.Encapsulate(localTransformPosition);

            center = b.center;
            size = b.size;
            return true;
        }

        //Editing Gizmo
        //[DrawGizmo(GizmoType.Active)]
        static void RenderGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            HDAdditionalReflectionData reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();
            if (s_LastInteractedEditorOld == null)
                return;

            if (s_LastInteractedEditorOld.sceneViewEditing && EditMode.editMode == EditMode.SceneViewEditMode.ReflectionProbeBox)
            {
                Color oldColor = Gizmos.color;
                Gizmos.color = kGizmoReflectionProbe;

                Gizmos.matrix = GetLocalSpace(reflectionProbe);
                if(reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Box)
                    Gizmos.DrawCube(reflectionProbe.center, -1f * reflectionProbe.size);
                if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Sphere)
                    Gizmos.DrawSphere(reflectionProbe.center, reflectionData.m_InfluenceSphereRadius);
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = oldColor;
            }
        }

        //[DrawGizmo(GizmoType.Selected)]
        static void DrawSelectedGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            Color oldColor = Gizmos.color;
            Gizmos.color = reflectionProbe.isActiveAndEnabled ? kGizmoReflectionProbe : kGizmoReflectionProbeDisabled;
            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();

            if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Box)
            {
                DrawBoxInfluenceGizmo(reflectionProbe, oldColor);
            }
            if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Sphere)
            {
                DrawSphereInfluenceGizmo(reflectionProbe, oldColor, reflectionData);
            }
            if (reflectionData.m_UseSeparateProjectionVolume)
            {
                DrawReprojectionVolumeGizmo(reflectionProbe, reflectionData);
            }
            Gizmos.color = oldColor;

            DrawVerticalRay(reflectionProbe.transform);

            reflectionData.ChangeVisibility(true);
        }

        static void DrawVerticalRay(Transform transform)
        {
            Ray ray = new Ray(transform.position, Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Handles.color = Color.green;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.DrawLine(transform.position - Vector3.up * 0.5f, hit.point);
                Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);

                Handles.color = Color.red;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.DrawLine(transform.position, hit.point);
                Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);
            }
        }

        //[DrawGizmo(GizmoType.NonSelected)]
        static void DrawNonSelectedGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();
            if (reflectionData != null)
                reflectionData.ChangeVisibility(false);
        }

        static void DrawReprojectionVolumeGizmo(ReflectionProbe reflectionProbe, HDAdditionalReflectionData reflectionData)
        {
            Color reprojectionColor = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.3f);
            Gizmos.color = reprojectionColor;
            Gizmos.matrix = GetLocalSpace(reflectionProbe);
            if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Box)
            {
                Gizmos.DrawWireCube(reflectionData.m_BoxReprojectionVolumeCenter, reflectionData.m_BoxReprojectionVolumeSize);
            }
            if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Sphere)
            {
                Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.m_SphereReprojectionVolumeRadius);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }

        static void DrawBoxInfluenceGizmo(ReflectionProbe reflectionProbe, Color oldColor)
        {
            Gizmos.matrix = GetLocalSpace(reflectionProbe);
            Gizmos.DrawWireCube(reflectionProbe.center, reflectionProbe.size);
            if (reflectionProbe.blendDistance > 0)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireCube(reflectionProbe.center, new Vector3(reflectionProbe.size.x - reflectionProbe.blendDistance * 2, reflectionProbe.size.y - reflectionProbe.blendDistance * 2, reflectionProbe.size.z - reflectionProbe.blendDistance * 2));
            }
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = oldColor;
        }

        static void DrawSphereInfluenceGizmo(ReflectionProbe reflectionProbe, Color oldColor, HDAdditionalReflectionData reflectionData)
        {
            Gizmos.matrix = GetLocalSpace(reflectionProbe);
            Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.m_InfluenceSphereRadius);
            if (reflectionProbe.blendDistance > 0)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.m_InfluenceSphereRadius - 2 * reflectionProbe.blendDistance);
            }
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = oldColor;
        }

        public void OnSceneGUI()
        {
            if (!sceneViewEditing)
                return;

            switch (EditMode.editMode)
            {
                case EditMode.SceneViewEditMode.ReflectionProbeBox:
                    if (m_InfluenceShape.enumValueIndex == 0)
                        DoInfluenceBoxEditing();
                    if (m_InfluenceShape.enumValueIndex == 1)
                        DoInfluenceSphereEditing();
                    break;
                case EditMode.SceneViewEditMode.GridBox:
                    if (m_InfluenceShape.enumValueIndex == 0)
                        DoProjectionBoxEditing();
                    if (m_InfluenceShape.enumValueIndex == 1)
                        DoProjectionSphereEditing();
                    break;
                case EditMode.SceneViewEditMode.Collider:
                    DoFitVolume();
                    break;
                case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                    DoOriginEditing();
                    break;
            }
        }

        void UpdateOldLocalSpace()
        {
            m_OldLocalSpace = GetLocalSpace(reflectionProbeTarget);
        }

        void DoOriginEditing()
        {
            ReflectionProbe p = reflectionProbeTarget;
            Vector3 transformPosition = p.transform.position;
            Vector3 size = p.size;

            EditorGUI.BeginChangeCheck();
            Vector3 newPostion = Handles.PositionHandle(transformPosition, GetLocalSpaceRotation(p));

            bool changed = EditorGUI.EndChangeCheck();

            if (changed || m_OldLocalSpace != GetLocalSpace(reflectionProbeTarget))
            {
                Vector3 localNewPosition = m_OldLocalSpace.inverse.MultiplyPoint3x4(newPostion);

                Bounds b = new Bounds(p.center, size);
                localNewPosition = b.ClosestPoint(localNewPosition);

                Undo.RecordObject(p.transform, "Modified Reflection Probe Origin");
                p.transform.position = m_OldLocalSpace.MultiplyPoint3x4(localNewPosition);

                Undo.RecordObject(p, "Modified Reflection Probe Origin");
                p.center = GetLocalSpace(p).inverse.MultiplyPoint3x4(m_OldLocalSpace.MultiplyPoint3x4(p.center));

                EditorUtility.SetDirty(target);

                UpdateOldLocalSpace();
            }
        }

        static Matrix4x4 GetLocalSpace(ReflectionProbe probe)
        {
            Vector3 t = probe.transform.position;
            return Matrix4x4.TRS(t, GetLocalSpaceRotation(probe), Vector3.one);
        }

        static Quaternion GetLocalSpaceRotation(ReflectionProbe probe)
        {
            bool supportsRotation = (SupportedRenderingFeatures.active.reflectionProbeSupportFlags & SupportedRenderingFeatures.ReflectionProbeSupportFlags.Rotation) != 0;
            if (supportsRotation)
                return probe.transform.rotation;
            else
                return Quaternion.identity;
        }

        void DoInfluenceBoxEditing()
        {
            ReflectionProbe p = reflectionProbeTarget;

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                m_BoxInfluenceBoundsHandle.center = p.center;
                m_BoxInfluenceBoundsHandle.size = p.size;
                m_BoxBlendHandle.center = p.center;
                m_BoxBlendHandle.size = p.size - Vector3.one * p.blendDistance * 2;

                EditorGUI.BeginChangeCheck();
                m_BoxInfluenceBoundsHandle.DrawHandle();
                m_BoxBlendHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(p, "Modified Reflection Probe AABB");
                    Vector3 center = m_BoxInfluenceBoundsHandle.center;
                    Vector3 size = m_BoxInfluenceBoundsHandle.size;
                    float blendDistance = ((p.size.x - m_BoxBlendHandle.size.x) / 2 + (p.size.y - m_BoxBlendHandle.size.y) / 2 + (p.size.z - m_BoxBlendHandle.size.z) / 2) / 3;
                    ValidateAABB(ref center, ref size);
                    p.center = center;
                    p.size = size;
                    p.blendDistance = Mathf.Max( blendDistance,0);
                    EditorUtility.SetDirty(target);
                }
            }
        }

        void DoProjectionBoxEditing()
        {
            ReflectionProbe p = reflectionProbeTarget;
            HDAdditionalReflectionData reflectionData = p.GetComponent<HDAdditionalReflectionData>();

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                m_BoxProjectionBoundsHandle.center = reflectionData.m_BoxReprojectionVolumeCenter;
                m_BoxProjectionBoundsHandle.size = reflectionData.m_BoxReprojectionVolumeSize;

                EditorGUI.BeginChangeCheck();
                m_BoxProjectionBoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection Probe AABB");
                    Vector3 center = m_BoxProjectionBoundsHandle.center;
                    Vector3 size = m_BoxProjectionBoundsHandle.size;
                    ValidateAABB(ref center, ref size);
                    reflectionData.m_BoxReprojectionVolumeCenter = center;
                    reflectionData.m_BoxReprojectionVolumeSize = size;
                    EditorUtility.SetDirty(reflectionData);
                }
            }
        }

        void DoInfluenceSphereEditing()
        {
            ReflectionProbe p = reflectionProbeTarget;
            HDAdditionalReflectionData reflectionData = p.GetComponent<HDAdditionalReflectionData>();

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                m_InfluenceSphereHandle.center = p.center;
                m_InfluenceSphereHandle.radius = reflectionData.m_InfluenceSphereRadius;
                m_SphereBlendHandle.center = p.center;
                m_SphereBlendHandle.radius = Mathf.Min(reflectionData.m_InfluenceSphereRadius - p.blendDistance * 2, reflectionData.m_InfluenceSphereRadius);

                EditorGUI.BeginChangeCheck();
                m_InfluenceSphereHandle.DrawHandle();
                m_SphereBlendHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection influence volume");
                    Vector3 center = m_InfluenceSphereHandle.center;
                    Vector3 radius = new Vector3(m_InfluenceSphereHandle.radius, m_InfluenceSphereHandle.radius, m_InfluenceSphereHandle.radius);
                    float blendDistance = (m_InfluenceSphereHandle.radius - m_SphereBlendHandle.radius) / 2;
                    ValidateAABB(ref center, ref radius);
                    reflectionData.m_InfluenceSphereRadius = radius.x;
                    p.blendDistance = blendDistance;
                    EditorUtility.SetDirty(target);
                    EditorUtility.SetDirty(reflectionData);
                }
            }
        }

        void DoProjectionSphereEditing()
        {
            ReflectionProbe p = reflectionProbeTarget;
            HDAdditionalReflectionData reflectionData = p.GetComponent<HDAdditionalReflectionData>();

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                m_ProjectionSphereHandle.center = p.center;
                m_ProjectionSphereHandle.radius = reflectionData.m_SphereReprojectionVolumeRadius;

                EditorGUI.BeginChangeCheck();
                m_ProjectionSphereHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection Probe projection volume");
                    Vector3 center = m_ProjectionSphereHandle.center;
                    float radius = m_ProjectionSphereHandle.radius;
                    //ValidateAABB(ref center, ref radius);
                    reflectionData.m_SphereReprojectionVolumeRadius = radius;
                    EditorUtility.SetDirty(reflectionData);
                }
            }
        }

        void DoFitVolume()
        {
            var hitPositionsList = new List<Vector3>();

            for (int i = 0; i < rayCount; i++)
            {
                Vector3 randomDirection = Random.insideUnitSphere;
                Ray ray = new Ray(reflectionProbeTarget.transform.position + Random.insideUnitSphere, randomDirection);
                RaycastHit hit;
                var hits = Physics.RaycastAll(ray, m_MaxSearchDistance.floatValue);

                for (int j=0;j<hits.Length;j++)
                {
                    if (!hits[j].collider.isTrigger)
                    {
                        hitPositionsList.Add(hits[j].point);
                        //Debug visualization
                        if (drawFitFunctionDebug)
                        {
                            Debug.DrawLine(ray.origin, hits[j].point, Color.red, 1f);
                            Debug.DrawLine(hits[j].point, hits[j].point + hits[j].normal * 0.5f, Color.green, 5);
                            break;
                        }
                    }
                }
            }

            if (hitPositionsList.Count > 0)
            {
                //int averageSamples = 5;
                int averageSamples = Mathf.CeilToInt(rayCount * 2.5f / 100);
                Debug.Log("average on " + averageSamples + " samples");
                Vector3 upperEstimatedBounds = Vector3.zero;
                Vector3 lowerEstimatedBounds = Vector3.zero;
                // Sort Y
                hitPositionsList.Sort(new VectorSorter(Vector3.up));
                for (int i = hitPositionsList.Count - 1; i > hitPositionsList.Count - 1 - averageSamples; i--)
                {
                    upperEstimatedBounds.y += hitPositionsList[i].y / averageSamples;
                }
                for (int i = 0; i < averageSamples; i++)
                {
                    lowerEstimatedBounds.y += hitPositionsList[i].y / averageSamples;
                }
                // Sort X
                hitPositionsList.Sort(new VectorSorter(Vector3.right));
                for (int i = hitPositionsList.Count - 1; i > hitPositionsList.Count - 1 - averageSamples; i--)
                {
                    upperEstimatedBounds.x += hitPositionsList[i].x / averageSamples;
                }
                for (int i = 0; i < averageSamples; i++)
                {
                    lowerEstimatedBounds.x += hitPositionsList[i].x / averageSamples;
                }
                // Sort Z
                hitPositionsList.Sort(new VectorSorter(Vector3.forward));
                for (int i = hitPositionsList.Count - 1; i > hitPositionsList.Count - 1 - averageSamples; i--)
                {
                    upperEstimatedBounds.z += hitPositionsList[i].z / averageSamples;
                }
                for (int i = 0; i < averageSamples; i++)
                {
                    lowerEstimatedBounds.z += hitPositionsList[i].z / averageSamples;
                }

                //ShowList(hitPositionsList);

                HDAdditionalReflectionData reflectionData = reflectionProbeTarget.GetComponent<HDAdditionalReflectionData>();

                if (drawFitFunctionDebug)
                {
                    Debug.Log("upper"+upperEstimatedBounds);
                    Debug.Log("lower"+lowerEstimatedBounds);
                }

                if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Box)
                {
                    Vector3 newSize = upperEstimatedBounds - lowerEstimatedBounds;
                    newSize = new Vector3(Mathf.Abs(newSize.x), Mathf.Abs(newSize.y), Mathf.Abs(newSize.z));
                    Vector3 newCenter = Vector3.Lerp(upperEstimatedBounds,lowerEstimatedBounds,0.5f);
                    newCenter = newCenter - reflectionProbeTarget.transform.position;
                    if(reflectionData.m_UseSeparateProjectionVolume)
                    {
                        reflectionData.m_BoxReprojectionVolumeSize = newSize;
                        reflectionData.m_BoxReprojectionVolumeCenter = newCenter;
                    }
                    if(!reflectionData.m_UseSeparateProjectionVolume)
                    {
                        newSize += Vector3.one * reflectionProbeTarget.blendDistance;
                        reflectionProbeTarget.size = newSize;
                        reflectionProbeTarget.center = newCenter;
                    }
                    Debug.Log("newsize" + newSize);
                    Debug.Log("newcenter" + newCenter);
                    if(drawFitFunctionDebug)
                        Debug.DrawLine(reflectionProbeTarget.transform.position + newCenter, reflectionProbeTarget.transform.position + newCenter + Vector3.up * 0.1f,Color.red,5f);
                    
                }
                if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Sphere)
                {
                    float newSize = (Mathf.Abs(upperEstimatedBounds.x) + Mathf.Abs(upperEstimatedBounds.y) + Mathf.Abs(upperEstimatedBounds.z) + Mathf.Abs(lowerEstimatedBounds.x) + Mathf.Abs(lowerEstimatedBounds.y) + Mathf.Abs(lowerEstimatedBounds.z)) / 3;
                    reflectionData.m_SphereReprojectionVolumeRadius = newSize;
                }
            }
            else
                Debug.Log("No successful hits, do you have colliders in the scene ?");
            EditMode.ChangeEditMode(EditMode.SceneViewEditMode.None, new Bounds(), this);

        }

        class VectorSorter : IComparer<Vector3>
        {
            private Vector3 baseAxis;

            public VectorSorter(Vector3 baseAxis) {
                this.baseAxis = baseAxis;
            }

            public int Compare(Vector3 a, Vector3 b)
            {
                return (new Vector3(baseAxis.x * a.x, baseAxis.y * a.y, baseAxis.z * a.z)).magnitude.CompareTo((new Vector3(baseAxis.x * b.x, baseAxis.y * b.y, baseAxis.z * b.z)).magnitude);
            }
        }

        public void ShowList(List<Vector3> list)
        {
            Debug.Log("-------List-------");
            foreach (Vector3 v in list)
                Debug.Log(v);
        }

    }
}
