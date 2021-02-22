using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using static UnityEditorInternal.EditMode;

namespace UnityEditor.Rendering
{
    public partial class DecalEditorBase : Editor
    {
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

        static List<DecalEditorBase> s_Instances = new List<DecalEditorBase>();

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
    }
}
