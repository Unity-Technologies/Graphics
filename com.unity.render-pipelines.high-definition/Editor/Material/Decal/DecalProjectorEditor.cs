using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShortcutManagement;
using static UnityEditorInternal.EditMode;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(DecalProjector))]
    [CanEditMultipleObjects]
    partial class DecalProjectorEditor : Editor
    {
        MaterialEditor m_MaterialEditor = null;
        SerializedProperty m_MaterialProperty;
        SerializedProperty m_DrawDistanceProperty;
        SerializedProperty m_FadeScaleProperty;
        SerializedProperty m_StartAngleFadeProperty;
        SerializedProperty m_EndAngleFadeProperty;
        SerializedProperty m_UVScaleProperty;
        SerializedProperty m_UVBiasProperty;
        SerializedProperty m_AffectsTransparencyProperty;
        SerializedProperty m_Size;
        SerializedProperty[] m_SizeValues;
        SerializedProperty m_OffsetZ;
        SerializedProperty m_FadeFactor;
        SerializedProperty m_DecalLayerMask;

        int layerMask => (target as Component).gameObject.layer;
        bool layerMaskHasMultipleValue
        {
            get
            {
                if (targets.Length < 2)
                    return false;
                int layerMask = (targets[0] as Component).gameObject.layer;
                for (int index = 0; index < targets.Length; ++index)
                {
                    if ((targets[index] as Component).gameObject.layer != layerMask)
                        return true;
                }
                return false;
            }
        }

        bool showAffectTransparency => ((target as DecalProjector).material != null) && DecalSystem.IsHDRenderPipelineDecal((target as DecalProjector).material.shader);

        bool showAffectTransparencyHaveMultipleDifferentValue
        {
            get
            {
                if (targets.Length < 2)
                    return false;
                DecalProjector decalProjector0 = (targets[0] as DecalProjector);
                bool show = decalProjector0.material != null && DecalSystem.IsHDRenderPipelineDecal(decalProjector0.material.shader);
                for (int index = 0; index < targets.Length; ++index)
                {
                    if ((targets[index] as DecalProjector).material != null)
                    {
                        DecalProjector decalProjectori = (targets[index] as DecalProjector);
                        if (decalProjectori != null && DecalSystem.IsHDRenderPipelineDecal(decalProjectori.material.shader) ^ show)
                            return true;
                    }
                }
                return false;
            }
        }

        static HierarchicalBox s_Handle;
        static HierarchicalBox handle
        {
            get
            {
                if (s_Handle == null || s_Handle.Equals(null))
                {
                    s_Handle = new HierarchicalBox(k_GizmoColorBase, k_BaseHandlesColor);
                    s_Handle.monoHandle = false;
                }
                return s_Handle;
            }
        }

        const SceneViewEditMode k_EditShapeWithoutPreservingUV = (SceneViewEditMode)90;
        const SceneViewEditMode k_EditShapePreservingUV = (SceneViewEditMode)91;
        const SceneViewEditMode k_EditUV = (SceneViewEditMode)92;
        static readonly SceneViewEditMode[] k_EditVolumeModes = new SceneViewEditMode[]
        {
            k_EditShapeWithoutPreservingUV,
            k_EditShapePreservingUV
        };
        static readonly SceneViewEditMode[] k_EditPivotModes = new SceneViewEditMode[]
        {
            k_EditUV
        };
        static SceneViewEditMode s_CurrentEditMode;
        static bool s_ModeSwitched;

        static GUIContent[] k_EditVolumeLabels = null;
        static GUIContent[] editVolumeLabels => k_EditVolumeLabels ?? (k_EditVolumeLabels = new GUIContent[]
        {
            EditorGUIUtility.TrIconContent("d_ScaleTool", k_EditShapeWithoutPreservingUVTooltip),
            EditorGUIUtility.TrIconContent("d_RectTool", k_EditShapePreservingUVTooltip)
        });
        static GUIContent[] k_EditPivotLabels = null;
        static GUIContent[] editPivotLabels => k_EditPivotLabels ?? (k_EditPivotLabels = new GUIContent[]
        {
            EditorGUIUtility.TrIconContent("d_MoveTool", k_EditUVTooltip)
        });

        static Editor s_Owner;

        private void OnEnable()
        {
            s_Owner = this;

            // Create an instance of the MaterialEditor
            UpdateMaterialEditor();
            foreach (var decalProjector in targets)
            {
                (decalProjector as DecalProjector).OnMaterialChange += RequireUpdateMaterialEditor;
            }

            // Fetch serialized properties
            m_MaterialProperty = serializedObject.FindProperty("m_Material");
            m_DrawDistanceProperty = serializedObject.FindProperty("m_DrawDistance");
            m_FadeScaleProperty = serializedObject.FindProperty("m_FadeScale");
            m_StartAngleFadeProperty = serializedObject.FindProperty("m_StartAngleFade");
            m_EndAngleFadeProperty = serializedObject.FindProperty("m_EndAngleFade");
            m_UVScaleProperty = serializedObject.FindProperty("m_UVScale");
            m_UVBiasProperty = serializedObject.FindProperty("m_UVBias");
            m_AffectsTransparencyProperty = serializedObject.FindProperty("m_AffectsTransparency");
            m_Size = serializedObject.FindProperty("m_Size");
            m_SizeValues = new[]
            {
                m_Size.FindPropertyRelative("x"),
                m_Size.FindPropertyRelative("y"),
                m_Size.FindPropertyRelative("z"),
            };
            m_OffsetZ = serializedObject.FindProperty("m_Offset").FindPropertyRelative("z");
            m_FadeFactor = serializedObject.FindProperty("m_FadeFactor");
            m_DecalLayerMask = serializedObject.FindProperty("m_DecalLayerMask");
        }

        private void OnDisable()
        {
            foreach (DecalProjector decalProjector in targets)
            {
                if (decalProjector != null)
                    decalProjector.OnMaterialChange -= RequireUpdateMaterialEditor;
            }
            s_Owner = null;
        }

        private void OnDestroy() =>
            DestroyImmediate(m_MaterialEditor);

        public bool HasFrameBounds()
        {
            return true;
        }

        public Bounds OnGetFrameBounds()
        {
            DecalProjector decalProjector = target as DecalProjector;

            return new Bounds(decalProjector.transform.position, handle.size);
        }

        private bool m_RequireUpdateMaterialEditor = false;

        private void RequireUpdateMaterialEditor() => m_RequireUpdateMaterialEditor = true;

        public void UpdateMaterialEditor()
        {
            int validMaterialsCount = 0;
            for (int index = 0; index < targets.Length; ++index)
            {
                DecalProjector decalProjector = (targets[index] as DecalProjector);
                if((decalProjector != null) && (decalProjector.material != null))
                    validMaterialsCount++;
            }
            // Update material editor with the new material
            UnityEngine.Object[] materials = new UnityEngine.Object[validMaterialsCount];
            validMaterialsCount = 0;
            for (int index = 0; index < targets.Length; ++index)
            {
                DecalProjector decalProjector = (targets[index] as DecalProjector);

                if((decalProjector != null) && (decalProjector.material != null))
                    materials[validMaterialsCount++] = (targets[index] as DecalProjector).material;
            }
            m_MaterialEditor = (MaterialEditor)CreateEditor(materials);
        }

        void OnSceneGUI()
        {
            //called on each targets
            DrawHandles();
        }

        void DrawHandles()
        {
            //Note: each target need to be handled individually to allow multi edition
            DecalProjector decalProjector = target as DecalProjector;

            if (editMode == k_EditShapePreservingUV || editMode == k_EditShapeWithoutPreservingUV)
            {
                using (new Handles.DrawingScope(Color.white, Matrix4x4.TRS(decalProjector.transform.position, decalProjector.transform.rotation, Vector3.one)))
                {
                    bool needToRefreshDecalProjector = false;

                    handle.center = decalProjector.offset;
                    handle.size = decalProjector.size;

                    Vector3 boundsSizePreviousOS = handle.size;
                    Vector3 boundsMinPreviousOS = handle.size * -0.5f + handle.center;

                    EditorGUI.BeginChangeCheck();
                    handle.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        needToRefreshDecalProjector = true;

                        // Adjust decal transform if handle changed.
                        Undo.RecordObject(decalProjector, "Decal Projector Change");

                        decalProjector.size = handle.size;
                        decalProjector.offset = handle.center;

                        Vector3 boundsSizeCurrentOS = handle.size;
                        Vector3 boundsMinCurrentOS = handle.size * -0.5f + handle.center;

                        if (editMode == k_EditShapePreservingUV)
                        {
                            // Treat decal projector bounds as a crop tool, rather than a scale tool.
                            // Compute a new uv scale and bias terms to pin decal projection pixels in world space, irrespective of projector bounds.
                            Vector2 uvScale = decalProjector.uvScale;
                            uvScale.x *= Mathf.Max(1e-5f, boundsSizeCurrentOS.x) / Mathf.Max(1e-5f, boundsSizePreviousOS.x);
                            uvScale.y *= Mathf.Max(1e-5f, boundsSizeCurrentOS.y) / Mathf.Max(1e-5f, boundsSizePreviousOS.y);
                            decalProjector.uvScale = uvScale;

                            Vector2 uvBias = decalProjector.uvBias;
                            uvBias.x += (boundsMinCurrentOS.x - boundsMinPreviousOS.x) / Mathf.Max(1e-5f, boundsSizeCurrentOS.x) * decalProjector.uvScale.x;
                            uvBias.y += (boundsMinCurrentOS.y - boundsMinPreviousOS.y) / Mathf.Max(1e-5f, boundsSizeCurrentOS.y) * decalProjector.uvScale.y;
                            decalProjector.uvBias = uvBias;
                        }

                        if (PrefabUtility.IsPartOfNonAssetPrefabInstance(decalProjector))
                        {
                            PrefabUtility.RecordPrefabInstancePropertyModifications(decalProjector);
                        }
                    }

                    // Automatically recenter our transform component if necessary.
                    // In order to correctly handle world-space snapping, we only perform this recentering when the user is no longer interacting with the gizmo.
                    if ((GUIUtility.hotControl == 0) && (decalProjector.offset != Vector3.zero))
                    {
                        needToRefreshDecalProjector = true;

                        // Both the DecalProjectorComponent, and the transform will be modified.
                        // The undo system will automatically group all RecordObject() calls here into a single action.
                        Undo.RecordObject(decalProjector.transform, "Decal Projector Change");

                        // Re-center the transform to the center of the decal projector bounds,
                        // while maintaining the world-space coordinates of the decal projector boundings vertices.
                        // Center of the decal projector is not the same of the HierarchicalBox as we want it to be on the z face as lights
                        decalProjector.transform.Translate(decalProjector.offset + new Vector3(0f, 0f, handle.size.z * -0.5f), Space.Self);

                        decalProjector.offset = new Vector3(0f, 0f, handle.size.z * 0.5f);
                        if (PrefabUtility.IsPartOfNonAssetPrefabInstance(decalProjector))
                        {
                            PrefabUtility.RecordPrefabInstancePropertyModifications(decalProjector);
                        }
                    }

                    if (needToRefreshDecalProjector)
                    {
                        // Smoothly update the decal image projected
                        DecalSystem.instance.UpdateCachedData(decalProjector.Handle, decalProjector.GetCachedDecalData());
                    }
                }
            }

            //[TODO: add editable pivot. Uncomment this when ready]
            //else if (editMode == k_EditUV)
            //{
            //    //here should be handles code to manipulate the pivot without changing the UV
            //}
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(DecalProjector decalProjector, GizmoType gizmoType)
        {
            //draw them scale independent
            using (new Handles.DrawingScope(Color.white, Matrix4x4.TRS(decalProjector.transform.position, decalProjector.transform.rotation, Vector3.one)))
            {
                handle.center = decalProjector.offset;
                handle.size = decalProjector.size;
                bool inEditMode = editMode == k_EditShapePreservingUV || editMode == k_EditShapeWithoutPreservingUV;
                handle.DrawHull(inEditMode);

                Quaternion arrowRotation = Quaternion.LookRotation(Vector3.down, Vector3.right);
                float arrowSize = decalProjector.size.z * 0.25f;
                Vector3 pivot = decalProjector.offset;
                Vector3 projectedPivot = pivot + decalProjector.size.z * 0.5f * Vector3.back;
                Handles.ArrowHandleCap(0, projectedPivot, Quaternion.identity, arrowSize, EventType.Repaint);

                //[TODO: add editable pivot. Uncomment this when ready]
                //draw pivot
                //Handles.SphereHandleCap(controlID, pivot, Quaternion.identity, 0.02f, EventType.Repaint);
                //Color c = Color.white;
                //c.a = 0.2f;
                //Handles.color = c;
                //Handles.DrawLine(projectedPivot, projectedPivot + decalProjector.m_Size.x * 0.5f * Vector3.right);
                //Handles.DrawLine(projectedPivot, projectedPivot + decalProjector.m_Size.y * 0.5f * Vector3.up);
                //Handles.DrawLine(projectedPivot, projectedPivot + decalProjector.m_Size.z * 0.5f * Vector3.forward);

                //draw UV and bolder edges
                using (new Handles.DrawingScope(Matrix4x4.TRS(decalProjector.transform.position - decalProjector.transform.rotation * (decalProjector.size * 0.5f + decalProjector.offset.z * Vector3.back), decalProjector.transform.rotation, Vector3.one)))
                {
                    if (inEditMode)
                    {
                        Vector2 size = new Vector2(
                            (decalProjector.uvScale.x > 100000 || decalProjector.uvScale.x < -100000 ? 0f : 1f / decalProjector.uvScale.x) * decalProjector.size.x,
                            (decalProjector.uvScale.y > 100000 || decalProjector.uvScale.y < -100000 ? 0f : 1f / decalProjector.uvScale.y) * decalProjector.size.y
                            );
                        Vector2 start = (Vector2)projectedPivot - new Vector2(decalProjector.uvBias.x * size.x, decalProjector.uvBias.y * size.y);
                        Handles.DrawDottedLines(
                            new Vector3[]
                            {
                                start,                              start + new Vector2(size.x, 0),
                                start + new Vector2(size.x, 0),     start + size,
                                start + size,                       start + new Vector2(0, size.y),
                                start + new Vector2(0, size.y),     start
                            },
                            5f);
                    }

                    Vector2 halfSize = decalProjector.size * .5f;
                    Vector2 halfSize2 = new Vector2(halfSize.x, -halfSize.y);
                    Vector2 center = (Vector2)projectedPivot + halfSize;
                    Handles.DrawLine(center - halfSize,  center - halfSize2, 3f);
                    Handles.DrawLine(center - halfSize2, center + halfSize, 3f);
                    Handles.DrawLine(center + halfSize,  center + halfSize2, 3f);
                    Handles.DrawLine(center + halfSize2, center - halfSize, 3f);
                }
            }
        }

        Bounds GetBoundsGetter()
        {
            var bounds = new Bounds();
            var decalTransform = ((Component)target).transform;
            bounds.Encapsulate(decalTransform.position);
            return bounds;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (m_RequireUpdateMaterialEditor)
            {
                UpdateMaterialEditor();
                m_RequireUpdateMaterialEditor = false;
            }

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                DoInspectorToolbar(k_EditVolumeModes, editVolumeLabels, GetBoundsGetter, this);

                //[TODO: add editable pivot. Uncomment this when ready]
                //DoInspectorToolbar(k_EditPivotModes, editPivotLabels, GetBoundsGetter, this);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                
                Rect rect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2, k_SizeContent));
                EditorGUI.BeginProperty(rect, k_SizeSubContent[0], m_SizeValues[0]);
                EditorGUI.BeginProperty(rect, k_SizeSubContent[1], m_SizeValues[1]);
                float[] size = new float[2] { m_SizeValues[0].floatValue, m_SizeValues[1].floatValue };
                EditorGUI.BeginChangeCheck();
                EditorGUI.MultiFloatField(rect, k_SizeContent, k_SizeSubContent, size);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SizeValues[0].floatValue = Mathf.Max(0, size[0]);
                    m_SizeValues[1].floatValue = Mathf.Max(0, size[1]);
                }
                EditorGUI.EndProperty();
                EditorGUI.EndProperty();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_SizeValues[2], k_ProjectionDepthContent);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SizeValues[2].floatValue = Mathf.Max(0, m_SizeValues[2].floatValue);
                    m_OffsetZ.floatValue = m_SizeValues[2].floatValue * 0.5f;
                }

                EditorGUILayout.PropertyField(m_MaterialProperty, k_MaterialContent);

                bool decalLayerEnabled = false;
                HDRenderPipelineAsset hdrp = HDRenderPipeline.currentAsset;
                if (hdrp != null)
                {
                    decalLayerEnabled = hdrp.currentPlatformRenderPipelineSettings.supportDecals && hdrp.currentPlatformRenderPipelineSettings.supportDecalLayers;
                    using (new EditorGUI.DisabledScope(!decalLayerEnabled))
                    {
                        EditorGUILayout.PropertyField(m_DecalLayerMask, k_DecalLayerMaskContent);
                    }
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_DrawDistanceProperty, k_DistanceContent);
                if (EditorGUI.EndChangeCheck() && m_DrawDistanceProperty.floatValue < 0f)
                    m_DrawDistanceProperty.floatValue = 0f;

                EditorGUILayout.PropertyField(m_FadeScaleProperty, k_FadeScaleContent);
                using (new EditorGUI.DisabledScope(!decalLayerEnabled))
                {
                    float angleFadeMinValue = m_StartAngleFadeProperty.floatValue;
                    float angleFadeMaxValue = m_EndAngleFadeProperty.floatValue;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.MinMaxSlider(k_AngleFadeContent, ref angleFadeMinValue, ref angleFadeMaxValue, 0.0f, 180.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_StartAngleFadeProperty.floatValue = angleFadeMinValue;
                        m_EndAngleFadeProperty.floatValue = angleFadeMaxValue;
                    }
                }

                if (!decalLayerEnabled)
                {
                    EditorGUILayout.HelpBox("Enable 'Decal Layers' in your HDRP Asset if you want to control the Angle Fade. There is a performance cost of enabling this option.",
                    MessageType.Info);
                }

                EditorGUILayout.PropertyField(m_UVScaleProperty, k_UVScaleContent);
                EditorGUILayout.PropertyField(m_UVBiasProperty, k_UVBiasContent);
                EditorGUILayout.PropertyField(m_FadeFactor, k_FadeFactorContent);

                // only display the affects transparent property if material is HDRP/decal
                if (showAffectTransparencyHaveMultipleDifferentValue)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Multiple material type in selection"));
                }
                else if (showAffectTransparency)
                    EditorGUILayout.PropertyField(m_AffectsTransparencyProperty, k_AffectTransparentContent);
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (layerMaskHasMultipleValue || layerMask != (target as Component).gameObject.layer)
            {
                foreach (var decalProjector in targets)
                {
                    (decalProjector as DecalProjector).OnValidate();
                }
            }

            if (m_MaterialEditor != null)
            {
                // We need to prevent the user to edit default decal materials
                bool isDefaultMaterial = false;
                bool isValidDecalMaterial = true;
                var hdrp = HDRenderPipeline.currentAsset;
                if (hdrp != null)
                {
                    foreach(var decalProjector in targets)
                    {
                        var mat = (decalProjector as DecalProjector).material;

                        isDefaultMaterial |= mat == hdrp.GetDefaultDecalMaterial();
                        isValidDecalMaterial &= mat != null && DecalSystem.IsDecalMaterial(mat);
                    }
                }

                if (isValidDecalMaterial)
                {
                    // Draw the material's foldout and the material shader field
                    // Required to call m_MaterialEditor.OnInspectorGUI ();
                    m_MaterialEditor.DrawHeader();

                    using (new EditorGUI.DisabledGroupScope(isDefaultMaterial))
                    {
                        // Draw the material properties
                        // Works only if the foldout of m_MaterialEditor.DrawHeader () is open
                        m_MaterialEditor.OnInspectorGUI();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Decal only work with Decal Material. Decal Material can be selected in the shader list HDRP/Decal or can be created from a Decal Master Node.",
                        MessageType.Error);
                }
            }
        }

        [Shortcut("HDRP/Decal: Handle changing size stretching UV", typeof(SceneView), KeyCode.Keypad1, ShortcutModifiers.Action)]
        static void EnterEditModeWithoutPreservingUV(ShortcutArguments args)
        {
            //If editor is not there, then the selected GameObject does not contains a DecalProjector
            if (s_Owner == null || s_Owner.Equals(null))
                return;

            ChangeEditMode(k_EditShapeWithoutPreservingUV, (s_Owner as DecalProjectorEditor).GetBoundsGetter(), s_Owner);
        }

        [Shortcut("HDRP/Decal: Handle changing size cropping UV", typeof(SceneView), KeyCode.Keypad2, ShortcutModifiers.Action)]
        static void EnterEditModePreservingUV(ShortcutArguments args)
        {
            //If editor is not there, then the selected GameObject does not contains a DecalProjector
            if (s_Owner == null || s_Owner.Equals(null))
                return;

            ChangeEditMode(k_EditShapePreservingUV, (s_Owner as DecalProjectorEditor).GetBoundsGetter(), s_Owner);
        }

        //[TODO: add editable pivot. Uncomment this when ready]
        //[Shortcut("HDRP/Decal: Handle changing pivot position while preserving UV position", typeof(SceneView), KeyCode.Keypad3, ShortcutModifiers.Action)]
        //static void EnterEditModePivotPreservingUV(ShortcutArguments args) =>
        //    ChangeEditMode(k_EditUV, (s_Owner as DecalProjectorComponentEditor).GetBoundsGetter(), s_Owner);

        [Shortcut("HDRP/Decal: Handle swap between cropping and stretching UV", typeof(SceneView), KeyCode.W, ShortcutModifiers.Action)]
        static void SwappingEditUVMode(ShortcutArguments args)
        {
            SceneViewEditMode targetMode = SceneViewEditMode.None;
            switch (editMode)
            {
                case k_EditShapePreservingUV:
                    targetMode = k_EditShapeWithoutPreservingUV;
                    break;
                case k_EditShapeWithoutPreservingUV:
                    targetMode = k_EditShapePreservingUV;
                    break;
            }
            if (targetMode != SceneViewEditMode.None)
                ChangeEditMode(targetMode, (s_Owner as DecalProjectorEditor).GetBoundsGetter(), s_Owner);
        }

        [Shortcut("HDRP/Decal: Stop Editing", typeof(SceneView), KeyCode.Keypad0, ShortcutModifiers.Action)]
        static void ExitEditMode(ShortcutArguments args) => QuitEditMode();
    }
}
