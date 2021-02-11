using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditorInternal.EditMode;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(DecalProjector))]
    [CanEditMultipleObjects]
    partial class DecalProjectorEditor : Editor
    {
        const float k_Limit = 100000;
        const float k_LimitInv = 1 / k_Limit;

        static Color fullColor
        {
            get
            {
                Color c = s_LastColor;
                c.a = 1;
                return c;
            }
        }
        static Color s_LastColor;
        static void UpdateColorsInHandlesIfRequired()
        {
            Color c = HDRenderPipelinePreferences.decalGizmoColor;
            if (c != s_LastColor)
            {
                if (s_BoxHandle != null && !s_BoxHandle.Equals(null))
                    s_BoxHandle = null;

                if (s_uvHandles != null && !s_uvHandles.Equals(null))
                    s_uvHandles.baseColor = c;

                s_LastColor = c;
            }
        }

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
        SerializedProperty m_Offset;
        SerializedProperty[] m_OffsetValues;
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

        static HierarchicalBox s_BoxHandle;
        static HierarchicalBox boxHandle
        {
            get
            {
                if (s_BoxHandle == null || s_BoxHandle.Equals(null))
                {
                    Color c = fullColor;
                    s_BoxHandle = new HierarchicalBox(s_LastColor, new[] { c, c, c, c, c, c });
                    s_BoxHandle.SetBaseColor(s_LastColor);
                    s_BoxHandle.monoHandle = false;
                }
                return s_BoxHandle;
            }
        }

        static DisplacableRectHandles s_uvHandles;
        static DisplacableRectHandles uvHandles
        {
            get
            {
                if (s_uvHandles == null || s_uvHandles.Equals(null))
                    s_uvHandles = new DisplacableRectHandles(s_LastColor);
                return s_uvHandles;
            }
        }

        static readonly BoxBoundsHandle s_AreaLightHandle =
            new BoxBoundsHandle { axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y };

        const SceneViewEditMode k_EditShapeWithoutPreservingUV = (SceneViewEditMode)90;
        const SceneViewEditMode k_EditShapePreservingUV = (SceneViewEditMode)91;
        const SceneViewEditMode k_EditUVAndPivot = (SceneViewEditMode)92;
        static readonly SceneViewEditMode[] k_EditVolumeModes = new SceneViewEditMode[]
        {
            k_EditShapeWithoutPreservingUV,
            k_EditShapePreservingUV
        };
        static readonly SceneViewEditMode[] k_EditUVAndPivotModes = new SceneViewEditMode[]
        {
            k_EditUVAndPivot
        };

        static Func<Vector3, Quaternion, Vector3> s_DrawPivotHandle;

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

        static List<DecalProjectorEditor> s_Instances = new List<DecalProjectorEditor>();

        static DecalProjectorEditor FindEditorFromSelection()
        {
            GameObject[] selection = Selection.gameObjects;
            DecalProjector[] selectionTargets = Selection.GetFiltered<DecalProjector>(SelectionMode.Unfiltered);

            foreach (DecalProjectorEditor editor in s_Instances)
            {
                if (selectionTargets.Length != editor.targets.Length)
                    continue;
                bool allOk = true;
                foreach (DecalProjector selectionTarget in selectionTargets)
                    if (!Array.Find(editor.targets, t => t == selectionTarget))
                    {
                        allOk = false;
                        break;
                    }
                if (!allOk)
                    continue;
                return editor;
            }
            return null;
        }

        private void OnEnable()
        {
            s_Instances.Add(this);

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
            m_Offset = serializedObject.FindProperty("m_Offset");
            m_OffsetValues = new[]
            {
                m_Offset.FindPropertyRelative("x"),
                m_Offset.FindPropertyRelative("y"),
                m_Offset.FindPropertyRelative("z"),
            };
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

            s_Instances.Remove(this);
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

            return new Bounds(decalProjector.transform.position, boxHandle.size);
        }

        private bool m_RequireUpdateMaterialEditor = false;

        private void RequireUpdateMaterialEditor() => m_RequireUpdateMaterialEditor = true;

        public void UpdateMaterialEditor()
        {
            int validMaterialsCount = 0;
            for (int index = 0; index < targets.Length; ++index)
            {
                DecalProjector decalProjector = (targets[index] as DecalProjector);
                if ((decalProjector != null) && (decalProjector.material != null))
                    validMaterialsCount++;
            }
            // Update material editor with the new material
            UnityEngine.Object[] materials = new UnityEngine.Object[validMaterialsCount];
            validMaterialsCount = 0;
            for (int index = 0; index < targets.Length; ++index)
            {
                DecalProjector decalProjector = (targets[index] as DecalProjector);

                if ((decalProjector != null) && (decalProjector.material != null))
                    materials[validMaterialsCount++] = (targets[index] as DecalProjector).material;
            }
            m_MaterialEditor = (MaterialEditor)CreateEditor(materials);
        }

        void OnSceneGUI()
        {
            //called on each targets
            DrawHandles();
        }

        void DrawBoxTransformationHandles(DecalProjector decalProjector)
        {
            using (new Handles.DrawingScope(fullColor, Matrix4x4.TRS(decalProjector.transform.position, decalProjector.transform.rotation, Vector3.one)))
            {
                Vector3 centerStart = decalProjector.pivot;
                boxHandle.center = centerStart;
                boxHandle.size = decalProjector.size;

                Vector3 boundsSizePreviousOS = boxHandle.size;
                Vector3 boundsMinPreviousOS = boxHandle.size * -0.5f + boxHandle.center;

                EditorGUI.BeginChangeCheck();
                boxHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    // Adjust decal transform if handle changed.
                    Undo.RecordObject(decalProjector, "Decal Projector Change");

                    decalProjector.size = boxHandle.size;
                    decalProjector.pivot += boxHandle.center - centerStart;

                    Vector3 boundsSizeCurrentOS = boxHandle.size;
                    Vector3 boundsMinCurrentOS = boxHandle.size * -0.5f + boxHandle.center;

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

                    // Smoothly update the decal image projected
                    DecalSystem.instance.UpdateCachedData(decalProjector.Handle, decalProjector.GetCachedDecalData());
                }
            }
        }

        void DrawPivotHandles(DecalProjector decalProjector)
        {
            using (new Handles.DrawingScope(fullColor, Matrix4x4.TRS(Vector3.zero, decalProjector.transform.rotation, Vector3.one)))
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = ProjectedTransform.DrawHandles(decalProjector.transform.position, .5f * decalProjector.size.z - decalProjector.pivot.z, decalProjector.transform.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new UnityEngine.Object[] { decalProjector, decalProjector.transform }, "Decal Projector Change");

                    decalProjector.pivot += Quaternion.Inverse(decalProjector.transform.rotation) * (decalProjector.transform.position - newPosition);
                    decalProjector.transform.position = newPosition;
                }
            }
        }

        void DrawUVHandles(DecalProjector decalProjector)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(decalProjector.transform.position + decalProjector.transform.rotation * (decalProjector.pivot - .5f * decalProjector.size), decalProjector.transform.rotation, Vector3.one)))
            {
                Vector2 uvSize = new Vector2(
                    (decalProjector.uvScale.x > k_Limit || decalProjector.uvScale.x < -k_Limit) ? 0f : decalProjector.size.x / decalProjector.uvScale.x,
                    (decalProjector.uvScale.y > k_Limit || decalProjector.uvScale.y < -k_Limit) ? 0f : decalProjector.size.y / decalProjector.uvScale.y
                );
                Vector2 uvCenter = uvSize * .5f - new Vector2(decalProjector.uvBias.x * uvSize.x, decalProjector.uvBias.y * uvSize.y);

                uvHandles.center = uvCenter;
                uvHandles.size = uvSize;

                EditorGUI.BeginChangeCheck();
                uvHandles.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(decalProjector, "Decal Projector Change");

                    Vector2 limit = new Vector2(Mathf.Abs(decalProjector.size.x * k_LimitInv), Mathf.Abs(decalProjector.size.y * k_LimitInv));
                    Vector2 uvScale = uvHandles.size;
                    for (int channel = 0; channel < 2; channel++)
                    {
                        if (Mathf.Abs(uvScale[channel]) > limit[channel])
                            uvScale[channel] = decalProjector.size[channel] / uvScale[channel];
                        else
                            uvScale[channel] = Mathf.Sign(decalProjector.size[channel]) * Mathf.Sign(uvScale[channel]) * k_Limit;
                    }
                    decalProjector.uvScale = uvScale;

                    var newUVStart = uvHandles.center - .5f * uvHandles.size;
                    decalProjector.uvBias = -new Vector2(
                        (uvHandles.size.x < k_LimitInv) && (uvHandles.size.x > -k_LimitInv) ? k_Limit * newUVStart.x / decalProjector.size.x : newUVStart.x / uvHandles.size.x, //parenthesis to force format tool
                        (uvHandles.size.y < k_LimitInv) && (uvHandles.size.y > -k_LimitInv) ? k_Limit * newUVStart.y / decalProjector.size.y : newUVStart.y / uvHandles.size.y  //parenthesis to force format tool
                    );
                }
            }
        }

        void DrawHandles()
        {
            DecalProjector decalProjector = target as DecalProjector;

            if (editMode == k_EditShapePreservingUV || editMode == k_EditShapeWithoutPreservingUV)
                DrawBoxTransformationHandles(decalProjector);
            else if (editMode == k_EditUVAndPivot)
            {
                DrawPivotHandles(decalProjector);
                DrawUVHandles(decalProjector);
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(DecalProjector decalProjector, GizmoType gizmoType)
        {
            UpdateColorsInHandlesIfRequired();

            const float k_DotLength = 5f;

            //draw them scale independent
            using (new Handles.DrawingScope(fullColor, Matrix4x4.TRS(decalProjector.transform.position, decalProjector.transform.rotation, Vector3.one)))
            {
                boxHandle.center = decalProjector.pivot;
                boxHandle.size = decalProjector.size;
                bool isVolumeEditMode = editMode == k_EditShapePreservingUV || editMode == k_EditShapeWithoutPreservingUV;
                bool isPivotEditMode = editMode == k_EditUVAndPivot;
                boxHandle.DrawHull(isVolumeEditMode);

                Vector3 pivot = Vector3.zero;
                Vector3 projectedPivot = new Vector3(0, 0, decalProjector.pivot.z - .5f * decalProjector.size.z);

                if (isPivotEditMode)
                {
                    Handles.DrawDottedLines(new[] { projectedPivot, pivot }, k_DotLength);
                }
                else
                {
                    float arrowSize = decalProjector.size.z * 0.25f;
                    Handles.ArrowHandleCap(0, projectedPivot, Quaternion.identity, arrowSize, EventType.Repaint);
                }

                //draw UV and bolder edges
                using (new Handles.DrawingScope(Matrix4x4.TRS(decalProjector.transform.position + decalProjector.transform.rotation * new Vector3(decalProjector.pivot.x, decalProjector.pivot.y, decalProjector.pivot.z - .5f * decalProjector.size.z), decalProjector.transform.rotation, Vector3.one)))
                {
                    Vector2 UVSize = new Vector2(
                        (decalProjector.uvScale.x > k_Limit || decalProjector.uvScale.x < -k_Limit) ? 0f : decalProjector.size.x / decalProjector.uvScale.x,
                        (decalProjector.uvScale.y > k_Limit || decalProjector.uvScale.y < -k_Limit) ? 0f : decalProjector.size.y / decalProjector.uvScale.y
                    );
                    Vector2 UVCenter = UVSize * .5f - new Vector2(decalProjector.uvBias.x * UVSize.x, decalProjector.uvBias.y * UVSize.y) - (Vector2)decalProjector.size * .5f;

                    uvHandles.center = UVCenter;
                    uvHandles.size = UVSize;
                    uvHandles.DrawRect(dottedLine: true, screenSpaceSize: k_DotLength);

                    uvHandles.center = default;
                    uvHandles.size = decalProjector.size;
                    uvHandles.DrawRect(dottedLine: false, thickness: 3f);
                }
            }
        }

        static Func<Bounds> GetBoundsGetter(DecalProjector decalProjector)
        {
            return () =>
            {
                var bounds = new Bounds();
                var decalTransform = decalProjector.transform;
                bounds.Encapsulate(decalTransform.position);
                return bounds;
            };
        }

        void UpdateSize(int axe, float newSize, float oldSize)
        {
            m_SizeValues[axe].floatValue = newSize;
            if (oldSize > Mathf.Epsilon)
                m_OffsetValues[axe].floatValue *= newSize / oldSize;
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
                DoInspectorToolbar(k_EditVolumeModes, editVolumeLabels, GetBoundsGetter(target as DecalProjector), this);
                DoInspectorToolbar(k_EditUVAndPivotModes, editPivotLabels, GetBoundsGetter(target as DecalProjector), this);
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
                    for (int i = 0; i < 2; ++i)
                        UpdateSize(i, Mathf.Max(0, size[i]), m_SizeValues[i].floatValue);
                }
                EditorGUI.EndProperty();
                EditorGUI.EndProperty();

                EditorGUI.BeginChangeCheck();
                float oldSizeZ = m_SizeValues[2].floatValue;
                EditorGUILayout.PropertyField(m_SizeValues[2], k_ProjectionDepthContent);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateSize(2, Mathf.Max(0, m_SizeValues[2].floatValue), oldSizeZ);
                }

                EditorGUILayout.PropertyField(m_Offset, k_Offset);

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
                    foreach (var decalProjector in targets)
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
            DecalProjector activeDecalProjector = Selection.activeGameObject?.GetComponent<DecalProjector>();
            if (activeDecalProjector == null || activeDecalProjector.Equals(null))
                return;

            ChangeEditMode(k_EditShapeWithoutPreservingUV, GetBoundsGetter(activeDecalProjector)(), FindEditorFromSelection());
        }

        [Shortcut("HDRP/Decal: Handle changing size cropping UV", typeof(SceneView), KeyCode.Keypad2, ShortcutModifiers.Action)]
        static void EnterEditModePreservingUV(ShortcutArguments args)
        {
            //If editor is not there, then the selected GameObject does not contains a DecalProjector
            DecalProjector activeDecalProjector = Selection.activeGameObject?.GetComponent<DecalProjector>();
            if (activeDecalProjector == null || activeDecalProjector.Equals(null))
                return;

            ChangeEditMode(k_EditShapePreservingUV, GetBoundsGetter(activeDecalProjector)(), FindEditorFromSelection());
        }

        [Shortcut("HDRP/Decal: Handle changing pivot position and UVs", typeof(SceneView), KeyCode.Keypad3, ShortcutModifiers.Action)]
        static void EnterEditModePivotPreservingUV(ShortcutArguments args)
        {
            //If editor is not there, then the selected GameObject does not contains a DecalProjector
            DecalProjector activeDecalProjector = Selection.activeGameObject?.GetComponent<DecalProjector>();
            if (activeDecalProjector == null || activeDecalProjector.Equals(null))
                return;

            ChangeEditMode(k_EditUVAndPivot, GetBoundsGetter(activeDecalProjector)(), FindEditorFromSelection());
        }

        [Shortcut("HDRP/Decal: Handle swap between cropping and stretching UV", typeof(SceneView), KeyCode.W, ShortcutModifiers.Action)]
        static void SwappingEditUVMode(ShortcutArguments args)
        {
            //If editor is not there, then the selected GameObject does not contains a DecalProjector
            DecalProjector activeDecalProjector = Selection.activeGameObject?.GetComponent<DecalProjector>();
            if (activeDecalProjector == null || activeDecalProjector.Equals(null))
                return;

            SceneViewEditMode targetMode = SceneViewEditMode.None;
            switch (editMode)
            {
                case k_EditShapePreservingUV:
                case k_EditUVAndPivot:
                    targetMode = k_EditShapeWithoutPreservingUV;
                    break;
                case k_EditShapeWithoutPreservingUV:
                    targetMode = k_EditShapePreservingUV;
                    break;
            }
            if (targetMode != SceneViewEditMode.None)
                ChangeEditMode(targetMode, GetBoundsGetter(activeDecalProjector)(), FindEditorFromSelection());
        }

        [Shortcut("HDRP/Decal: Stop Editing", typeof(SceneView), KeyCode.Keypad0, ShortcutModifiers.Action)]
        static void ExitEditMode(ShortcutArguments args)
        {
            //If editor is not there, then the selected GameObject does not contains a DecalProjector
            DecalProjector activeDecalProjector = Selection.activeGameObject?.GetComponent<DecalProjector>();
            if (activeDecalProjector == null || activeDecalProjector.Equals(null))
                return;

            QuitEditMode();
        }
    }
}
