using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using static UnityEditorInternal.EditMode;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.Rendering
{
    public partial class DecalEditorBase : Editor
    {
        protected const float k_Limit = 100000;
        protected const float k_LimitInv = 1 / k_Limit;

        protected static Color fullColor
        {
            get
            {
                Color c = s_LastColor;
                c.a = 1;
                return c;
            }
        }
        static Color s_LastColor;

        protected static void UpdateColorsInHandlesIfRequired()
        {
            // TO UPDATE : Need to move the color preference to core
            // Color c = HDRenderPipelinePreferences.decalGizmoColor;
            Color c = new Color(1, 1, 1, 8f / 255);
            if (c != s_LastColor)
            {
                if (s_BoxHandle != null && !s_BoxHandle.Equals(null))
                    s_BoxHandle = null;

                if (s_uvHandles != null && !s_uvHandles.Equals(null))
                    s_uvHandles.baseColor = c;

                s_LastColor = c;
            }
        }

        protected MaterialEditor m_MaterialEditor = null;
        SerializedProperty m_MaterialProperty;
        protected SerializedProperty m_StartAngleFadeProperty;
        protected SerializedProperty m_EndAngleFadeProperty;
        SerializedProperty m_UVScaleProperty;
        SerializedProperty m_UVBiasProperty;
        SerializedProperty m_Size;
        protected SerializedProperty[] m_SizeValues;
        SerializedProperty m_Offset;
        SerializedProperty[] m_OffsetValues;


        static HierarchicalBox s_BoxHandle;
        protected static HierarchicalBox boxHandle
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
        protected static DisplacableRectHandles uvHandles
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

        protected const SceneViewEditMode k_EditShapeWithoutPreservingUV = (SceneViewEditMode)90;
        protected const SceneViewEditMode k_EditShapePreservingUV = (SceneViewEditMode)91;
        protected const SceneViewEditMode k_EditUVAndPivot = (SceneViewEditMode)92;
        protected readonly SceneViewEditMode[] k_EditVolumeModes = new SceneViewEditMode[]
        {
            k_EditShapeWithoutPreservingUV,
            k_EditShapePreservingUV
        };
        protected static readonly SceneViewEditMode[] k_EditUVAndPivotModes = new SceneViewEditMode[]
        {
            k_EditUVAndPivot
        };

        static Func<Vector3, Quaternion, Vector3> s_DrawPivotHandle;

        static GUIContent[] k_EditVolumeLabels = null;
        protected static GUIContent[] editVolumeLabels => k_EditVolumeLabels ?? (k_EditVolumeLabels = new GUIContent[]
        {
            EditorGUIUtility.TrIconContent("d_ScaleTool", k_EditShapeWithoutPreservingUVTooltip),
            EditorGUIUtility.TrIconContent("d_RectTool", k_EditShapePreservingUVTooltip)
        });
        static GUIContent[] k_EditPivotLabels = null;
        protected static GUIContent[] editPivotLabels => k_EditPivotLabels ?? (k_EditPivotLabels = new GUIContent[]
        {
            EditorGUIUtility.TrIconContent("d_MoveTool", k_EditUVTooltip)
        });

        protected static List<DecalEditorBase> s_Instances = new List<DecalEditorBase>();

        protected static DecalEditorBase FindEditorFromSelection()
        {
            GameObject[] selection = Selection.gameObjects;
            DecalBase[] selectionTargets = Selection.GetFiltered<DecalBase>(SelectionMode.Unfiltered);

            foreach (DecalEditorBase editor in s_Instances)
            {
                if (selectionTargets.Length != editor.targets.Length)
                    continue;
                bool allOk = true;
                foreach (DecalBase selectionTarget in selectionTargets)
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

        protected virtual void OnEnable()
        {
            s_Instances.Add(this);

            // Create an instance of the MaterialEditor
            UpdateMaterialEditor();
            foreach (var decalBase in targets)
            {
                (decalBase as DecalBase).OnMaterialChange += RequireUpdateMaterialEditor;
            }

            // Fetch serialized properties
            m_MaterialProperty = serializedObject.FindProperty("m_Material");
            m_StartAngleFadeProperty = serializedObject.FindProperty("m_StartAngleFade");
            m_EndAngleFadeProperty = serializedObject.FindProperty("m_EndAngleFade");
            m_UVScaleProperty = serializedObject.FindProperty("m_UVScale");
            m_UVBiasProperty = serializedObject.FindProperty("m_UVBias");
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
        }

        private void OnDisable()
        {
            foreach (DecalBase decalBase in targets)
            {
                if (decalBase != null)
                    decalBase.OnMaterialChange -= RequireUpdateMaterialEditor;
            }

            s_Instances.Remove(this);
        }

        private void OnDestroy() =>
            DestroyImmediate(m_MaterialEditor);

        public void UpdateMaterialEditor()
        {
            int validMaterialsCount = 0;
            for (int index = 0; index < targets.Length; ++index)
            {
                DecalBase decalBase = (targets[index] as DecalBase);
                if ((decalBase != null) && (decalBase.material != null))
                    validMaterialsCount++;
            }
            // Update material editor with the new material
            UnityEngine.Object[] materials = new UnityEngine.Object[validMaterialsCount];
            validMaterialsCount = 0;
            for (int index = 0; index < targets.Length; ++index)
            {
                DecalBase decalBase = (targets[index] as DecalBase);

                if ((decalBase != null) && (decalBase.material != null))
                    materials[validMaterialsCount++] = (targets[index] as DecalBase).material;
            }
            m_MaterialEditor = (MaterialEditor)CreateEditor(materials);
        }

        private bool m_RequireUpdateMaterialEditor = false;

        private void RequireUpdateMaterialEditor() => m_RequireUpdateMaterialEditor = true;

        void UpdateSize(int axe, float newSize, float oldSize)
        {
            m_SizeValues[axe].floatValue = newSize;
            if (oldSize > Mathf.Epsilon)
                m_OffsetValues[axe].floatValue *= newSize / oldSize;
        }
        protected static Func<Bounds> GetBoundsGetter(DecalBase decalBase)
        {
            return () =>
            {
                var bounds = new Bounds();
                var decalTransform = decalBase.transform;
                bounds.Encapsulate(decalTransform.position);
                return bounds;
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                DoInspectorToolbar(k_EditVolumeModes, editVolumeLabels, GetBoundsGetter(target as DecalBase), this);
                DoInspectorToolbar(k_EditUVAndPivotModes, editPivotLabels, GetBoundsGetter(target as DecalBase), this);
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

                EditorGUILayout.PropertyField(m_UVScaleProperty, k_UVScaleContent);
                EditorGUILayout.PropertyField(m_UVBiasProperty, k_UVBiasContent);
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (m_RequireUpdateMaterialEditor)
            {
                UpdateMaterialEditor();
                m_RequireUpdateMaterialEditor = false;
            }
        }

        // Handles

        public bool HasFrameBounds()
        {
            return true;
        }

        public Bounds OnGetFrameBounds()
        {
            DecalBase decalProjector = target as DecalBase;

            return new Bounds(decalProjector.transform.position, boxHandle.size);
        }

        void OnSceneGUI()
        {
            //called on each targets
            DrawHandles();
        }

        void DrawBoxTransformationHandles(DecalBase decalProjector)
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

                    DecalUpdateCallback( decalProjector );
                }
            }
        }

        void DrawPivotHandles(DecalBase decalProjector)
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

        void DrawUVHandles(DecalBase decalProjector)
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
            DecalBase decalProjector = target as DecalBase;

            if (editMode == k_EditShapePreservingUV || editMode == k_EditShapeWithoutPreservingUV)
                DrawBoxTransformationHandles(decalProjector);
            else if (editMode == k_EditUVAndPivot)
            {
                DrawPivotHandles(decalProjector);
                DrawUVHandles(decalProjector);
            }
        }

        protected virtual void DecalUpdateCallback( DecalBase decalProjector )
        {
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(DecalBase decalProjector, GizmoType gizmoType)
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

        [Shortcut("RenderPipeline/Decal: Handle changing size stretching UV", typeof(SceneView), KeyCode.Keypad1, ShortcutModifiers.Action)]
        static void EnterEditModeWithoutPreservingUV(ShortcutArguments args)
        {
            //If editor is not there, then the selected GameObject does not contains a DecalProjector
            DecalBase activeDecalProjector = Selection.activeGameObject?.GetComponent<DecalBase>();
            if (activeDecalProjector == null || activeDecalProjector.Equals(null))
                return;

            ChangeEditMode(k_EditShapeWithoutPreservingUV, GetBoundsGetter(activeDecalProjector)(), FindEditorFromSelection());
        }

        [Shortcut("RenderPipeline/Decal: Handle changing size cropping UV", typeof(SceneView), KeyCode.Keypad2, ShortcutModifiers.Action)]
        static void EnterEditModePreservingUV(ShortcutArguments args)
        {
            //If editor is not there, then the selected GameObject does not contains a DecalProjector
            DecalBase activeDecalProjector = Selection.activeGameObject?.GetComponent<DecalBase>();
            if (activeDecalProjector == null || activeDecalProjector.Equals(null))
                return;

            ChangeEditMode(k_EditShapePreservingUV, GetBoundsGetter(activeDecalProjector)(), FindEditorFromSelection());
        }

        [Shortcut("RenderPipeline/Decal: Handle changing pivot position and UVs", typeof(SceneView), KeyCode.Keypad3, ShortcutModifiers.Action)]
        static void EnterEditModePivotPreservingUV(ShortcutArguments args)
        {
            //If editor is not there, then the selected GameObject does not contains a DecalProjector
            DecalBase activeDecalProjector = Selection.activeGameObject?.GetComponent<DecalBase>();
            if (activeDecalProjector == null || activeDecalProjector.Equals(null))
                return;

            ChangeEditMode(k_EditUVAndPivot, GetBoundsGetter(activeDecalProjector)(), FindEditorFromSelection());
        }

        [Shortcut("RenderPipeline/Decal: Handle swap between cropping and stretching UV", typeof(SceneView), KeyCode.W, ShortcutModifiers.Action)]
        static void SwappingEditUVMode(ShortcutArguments args)
        {
            //If editor is not there, then the selected GameObject does not contains a DecalProjector
            DecalBase activeDecalProjector = Selection.activeGameObject?.GetComponent<DecalBase>();
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

        [Shortcut("RenderPipeline/Decal: Stop Editing", typeof(SceneView), KeyCode.Keypad0, ShortcutModifiers.Action)]
        static void ExitEditMode(ShortcutArguments args)
        {
            //If editor is not there, then the selected GameObject does not contains a DecalProjector
            DecalBase activeDecalProjector = Selection.activeGameObject?.GetComponent<DecalBase>();
            if (activeDecalProjector == null || activeDecalProjector.Equals(null))
                return;

            QuitEditMode();
        }
    }
}
