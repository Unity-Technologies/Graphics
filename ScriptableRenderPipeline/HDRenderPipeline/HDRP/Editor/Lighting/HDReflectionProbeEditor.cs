using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [CustomEditorForRenderPipeline(typeof(ReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDReflectionProbeEditor : Editor
    {
        static Dictionary<ReflectionProbe, HDReflectionProbeEditor> s_ReflectionProbeEditors = new Dictionary<ReflectionProbe, HDReflectionProbeEditor>();

        static HDReflectionProbeEditor GetEditorFor(ReflectionProbe p)
        {
            HDReflectionProbeEditor e;
            if (s_ReflectionProbeEditors.TryGetValue(p, out e) 
                && e != null 
                && !e.Equals(null)
                && ArrayUtility.IndexOf(e.targets, p) != -1)
                return e;

            return null;
        }

        SerializedHDReflectionProbe m_SerializedHdReflectionProbe;
        SerializedObject m_AdditionalDataSerializedObject;
        HDReflectionProbeUI m_UIState = new HDReflectionProbeUI();

        int m_PositionHash = 0;

        public bool sceneViewEditing
        {
            get { return IsReflectionProbeEditMode(EditMode.editMode) && EditMode.IsOwner(this); }
        }

        void OnEnable()
        {
            var additionalData = CoreEditorUtils.GetAdditionalData<HDAdditionalReflectionData>(targets);
            m_AdditionalDataSerializedObject = new SerializedObject(additionalData);
            m_SerializedHdReflectionProbe = new SerializedHDReflectionProbe(serializedObject, m_AdditionalDataSerializedObject);
            m_UIState.owner = this;
            m_UIState.Reset(
                m_SerializedHdReflectionProbe,
                Repaint);

            foreach (var t in targets)
            {
                var p = (ReflectionProbe)t;
                s_ReflectionProbeEditors[p] = this;
            }

            InitializeAllTargetProbes();
            ChangeVisibilityOfAllTargets(true);
        }

        void OnDisable()
        {
            ChangeVisibilityOfAllTargets(false);
        }

        public override void OnInspectorGUI()
        {
            //InspectColorsGUI();

            var s = m_UIState;
            var p = m_SerializedHdReflectionProbe;

            s.Update();
            p.Update();

            HDReflectionProbeUI.Inspector.Draw(s, p, this);

            PerformOperations(s, p, this);

            p.Apply();

            HideAdditionalComponents(false);

            HDReflectionProbeUI.DoShortcutKey(p, this);
        }

        public static bool IsReflectionProbeEditMode(EditMode.SceneViewEditMode editMode)
        {
            return editMode == EditMode.SceneViewEditMode.ReflectionProbeBox || editMode == EditMode.SceneViewEditMode.Collider || editMode == EditMode.SceneViewEditMode.GridBox ||
                editMode == EditMode.SceneViewEditMode.ReflectionProbeOrigin;
        }

        static void PerformOperations(HDReflectionProbeUI s, SerializedHDReflectionProbe p, HDReflectionProbeEditor o)
        {
            
        }

        void HideAdditionalComponents(bool visible)
        {
            var adds = CoreEditorUtils.GetAdditionalData<HDAdditionalReflectionData>(targets);
            var flags = visible ? HideFlags.None : HideFlags.HideInInspector;
            for (var i = 0 ; i < targets.Length; ++i)
            {
                var target = targets[i];
                var addData = adds[i];
                var p = (ReflectionProbe)target;
                var meshRenderer = p.GetComponent<MeshRenderer>();
                var meshFilter = p.GetComponent<MeshFilter>();

                addData.hideFlags = flags;
                meshRenderer.hideFlags = flags;
                meshFilter.hideFlags = flags;
            }
        }

        void BakeRealtimeProbeIfPositionChanged(HDReflectionProbeUI s, SerializedHDReflectionProbe sp, Editor o)
        {
            if (Application.isPlaying
                || ((ReflectionProbeMode)sp.mode.intValue) != ReflectionProbeMode.Realtime)
            {
                m_PositionHash = 0;
                return;
            }

            var hash = 0;
            for (var i = 0; i < sp.so.targetObjects.Length; i++)
            {
                var p = (ReflectionProbe)sp.so.targetObjects[i];
                var tr = p.GetComponent<Transform>();
                hash ^= tr.position.GetHashCode();
            }

            if (hash != m_PositionHash)
            {
                m_PositionHash = hash;
                for (var i = 0; i < sp.so.targetObjects.Length; i++)
                {
                    var p = (ReflectionProbe)sp.so.targetObjects[i];
                    p.RenderProbe();
                }
            }
        }

        

        static void InspectColorsGUI()
        {
            EditorGUILayout.LabelField("Color Theme", EditorStyles.largeLabel);
            k_GizmoThemeColorExtent = EditorGUILayout.ColorField("Extent", k_GizmoThemeColorExtent);
            k_GizmoThemeColorExtentFace = EditorGUILayout.ColorField("Extent Face", k_GizmoThemeColorExtentFace);
            k_GizmoThemeColorInfluenceBlend = EditorGUILayout.ColorField("Influence Blend", k_GizmoThemeColorInfluenceBlend);
            k_GizmoThemeColorInfluenceBlendFace = EditorGUILayout.ColorField("Influence Blend Face", k_GizmoThemeColorInfluenceBlendFace);
            k_GizmoThemeColorInfluenceNormalBlend = EditorGUILayout.ColorField("Influence Normal Blend", k_GizmoThemeColorInfluenceNormalBlend);
            k_GizmoThemeColorInfluenceNormalBlendFace = EditorGUILayout.ColorField("Influence Normal Blend Face", k_GizmoThemeColorInfluenceNormalBlendFace);
            k_GizmoThemeColorProjection = EditorGUILayout.ColorField("Projection", k_GizmoThemeColorProjection);
            k_GizmoThemeColorProjectionFace = EditorGUILayout.ColorField("Projection Face", k_GizmoThemeColorProjectionFace);
            k_GizmoThemeColorDisabled = EditorGUILayout.ColorField("Disabled", k_GizmoThemeColorDisabled);
            k_GizmoThemeColorDisabledFace = EditorGUILayout.ColorField("Disabled Face", k_GizmoThemeColorDisabledFace);
            EditorGUILayout.Space();
        }

        static void ApplyConstraintsOnTargets(HDReflectionProbeUI s, SerializedHDReflectionProbe sp, Editor o)
        {
            switch ((ShapeType)sp.influenceShape.enumValueIndex)
            {
                case ShapeType.Box:
                {
                    var maxBlendDistance = HDReflectionProbeEditorUtility.CalculateBoxMaxBlendDistance(s, sp, o);
                    sp.targetData.blendDistancePositive = Vector3.Min(sp.targetData.blendDistancePositive, maxBlendDistance);
                    sp.targetData.blendDistanceNegative = Vector3.Min(sp.targetData.blendDistanceNegative, maxBlendDistance);
                    sp.targetData.blendNormalDistancePositive = Vector3.Min(sp.targetData.blendNormalDistancePositive, maxBlendDistance);
                    sp.targetData.blendNormalDistanceNegative = Vector3.Min(sp.targetData.blendNormalDistanceNegative, maxBlendDistance);
                    break;
                }
                case ShapeType.Sphere:
                {
                    var maxBlendDistance = Vector3.one * HDReflectionProbeEditorUtility.CalculateSphereMaxBlendDistance(s, sp, o);
                    sp.targetData.blendDistancePositive = Vector3.Min(sp.targetData.blendDistancePositive, maxBlendDistance);
                    sp.targetData.blendDistanceNegative = Vector3.Min(sp.targetData.blendDistanceNegative, maxBlendDistance);
                    sp.targetData.blendNormalDistancePositive = Vector3.Min(sp.targetData.blendNormalDistancePositive, maxBlendDistance);
                    sp.targetData.blendNormalDistanceNegative = Vector3.Min(sp.targetData.blendNormalDistanceNegative, maxBlendDistance);
                    break;
                }
            }
        }
    }
}
