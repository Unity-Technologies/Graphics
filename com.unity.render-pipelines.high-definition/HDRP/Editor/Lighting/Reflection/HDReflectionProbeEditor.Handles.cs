using System;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDReflectionProbeEditor
    {
        enum InfluenceType
        {
            Standard,
            Normal
        }

        internal static Color k_GizmoThemeColorExtent = new Color(255f / 255f, 229f / 255f, 148f / 255f, 80f / 255f);
        internal static Color k_GizmoThemeColorExtentFace = new Color(255f / 255f, 229f / 255f, 148f / 255f, 45f / 255f);
        internal static Color k_GizmoThemeColorInfluenceBlend = new Color(83f / 255f, 255f / 255f, 95f / 255f, 75f / 255f);
        internal static Color k_GizmoThemeColorInfluenceBlendFace = new Color(83f / 255f, 255f / 255f, 95f / 255f, 17f / 255f);
        internal static Color k_GizmoThemeColorInfluenceNormalBlend = new Color(0f / 255f, 229f / 255f, 255f / 255f, 80f / 255f);
        internal static Color k_GizmoThemeColorInfluenceNormalBlendFace = new Color(0f / 255f, 229f / 255f, 255f / 255f, 36f / 255f);
        internal static Color k_GizmoThemeColorProjection = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        internal static Color k_GizmoThemeColorProjectionFace = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        internal static Color k_GizmoThemeColorDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);
        internal static Color k_GizmoThemeColorDisabledFace = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);

        internal static readonly Color[][] k_handlesColor = new Color[][]
        {
            new Color[]
            {
                Color.red,
                Color.green,
                Color.blue
            },
            new Color[]
            {
                new Color(.5f, 0f, 0f, 1f),
                new Color(0f, .5f, 0f, 1f),
                new Color(0f, 0f, .5f, 1f)
            }
        };

        void OnSceneGUI()
        {
            var s = m_UIState;
            var p = m_SerializedHdReflectionProbe;
            var o = this;

            BakeRealtimeProbeIfPositionChanged(s, p, o);

            HDReflectionProbeUI.DoShortcutKey(p, o);

            if (!s.sceneViewEditing)
                return;

            EditorGUI.BeginChangeCheck();

            switch (EditMode.editMode)
            {
                // Influence editing
                case EditMode.SceneViewEditMode.ReflectionProbeBox:
                    Handle_InfluenceEditing(s, p, o);
                    break;
                // Influence fade editing
                case EditMode.SceneViewEditMode.GridBox:
                    Handle_InfluenceFadeEditing(s, p, o, InfluenceType.Standard);
                    break;
                // Influence normal fade editing
                case EditMode.SceneViewEditMode.Collider:
                    Handle_InfluenceFadeEditing(s, p, o, InfluenceType.Normal);
                    break;
                // Origin editing
                case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                    Handle_OriginEditing(s, p, o);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
                Repaint();
        }

        static void Handle_InfluenceFadeEditing(HDReflectionProbeUI s, SerializedHDReflectionProbe sp, Editor o, InfluenceType influenceType)
        {
            Gizmo6FacesBoxContained alternativeBlendBox;
            SphereBoundsHandle sphereHandle;
            Vector3 probeBlendDistancePositive, probeBlendDistanceNegative;
            Color color;
            switch (influenceType)
            {
                default:
                case InfluenceType.Standard:
                    {
                        alternativeBlendBox = s.alternativeBoxBlendHandle;
                        sphereHandle = s.sphereBlendHandle;
                        probeBlendDistancePositive = sp.targetData.blendDistancePositive;
                        probeBlendDistanceNegative = sp.targetData.blendDistanceNegative;
                        color = k_GizmoThemeColorInfluenceBlend;
                        break;
                    }
                case InfluenceType.Normal:
                    {
                        alternativeBlendBox = s.alternativeBoxBlendNormalHandle;
                        sphereHandle = s.sphereBlendNormalHandle;
                        probeBlendDistancePositive = sp.targetData.blendNormalDistancePositive;
                        probeBlendDistanceNegative = sp.targetData.blendNormalDistanceNegative;
                        color = k_GizmoThemeColorInfluenceNormalBlend;
                        break;
                    }
            }

            var mat = Handles.matrix;
            var col = Handles.color;
            Handles.matrix = HDReflectionProbeEditorUtility.GetLocalSpace(sp.target);
            switch ((ShapeType)sp.influenceShape.enumValueIndex)
            {
                case ShapeType.Box:
                    {
                        alternativeBlendBox.center = sp.target.center - (probeBlendDistancePositive - probeBlendDistanceNegative) * 0.5f;
                        alternativeBlendBox.size = sp.target.size - probeBlendDistancePositive - probeBlendDistanceNegative;

                        alternativeBlendBox.container.center = sp.target.center;
                        alternativeBlendBox.container.size = sp.target.size;

                        EditorGUI.BeginChangeCheck();
                        alternativeBlendBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(sp.target, "Modified Reflection Probe Influence");
                            Undo.RecordObject(sp.targetData, "Modified Reflection Probe Influence");

                            var center = sp.target.center;
                            var influenceSize = sp.target.size;

                            var diff = 2 * (alternativeBlendBox.center - center);
                            var sum = influenceSize - (alternativeBlendBox.size);
                            var positive = (sum - diff) * 0.5f;
                            var negative = (sum + diff) * 0.5f;
                            var blendDistancePositive = Vector3.Max(Vector3.zero, Vector3.Min(positive, influenceSize));
                            var blendDistanceNegative = Vector3.Max(Vector3.zero, Vector3.Min(negative, influenceSize));

                            probeBlendDistancePositive = blendDistancePositive;
                            probeBlendDistanceNegative = blendDistanceNegative;

                            ApplyConstraintsOnTargets(s, sp, o);

                            EditorUtility.SetDirty(sp.target);
                            EditorUtility.SetDirty(sp.targetData);
                        }
                        break;
                    }
                case ShapeType.Sphere:
                    {
                        sphereHandle.center = sp.target.center;
                        sphereHandle.radius = Mathf.Clamp(sp.targetData.influenceSphereRadius - probeBlendDistancePositive.x, 0, sp.targetData.influenceSphereRadius);

                        Handles.color = k_GizmoThemeColorExtent;
                        EditorGUI.BeginChangeCheck();
                        Handles.color = color;
                        sphereHandle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(sp.target, "Modified Reflection influence volume");
                            Undo.RecordObject(sp.targetData, "Modified Reflection influence volume");

                            var influenceRadius = sp.targetData.influenceSphereRadius;
                            var blendRadius = sphereHandle.radius;

                            var blendDistance = Mathf.Clamp(influenceRadius - blendRadius, 0, influenceRadius);

                            probeBlendDistancePositive = Vector3.one * blendDistance;
                            probeBlendDistanceNegative = probeBlendDistancePositive;

                            ApplyConstraintsOnTargets(s, sp, o);

                            EditorUtility.SetDirty(sp.target);
                            EditorUtility.SetDirty(sp.targetData);
                        }
                        break;
                    }
            }
            Handles.matrix = mat;
            Handles.color = col;


            switch (influenceType)
            {
                default:
                case InfluenceType.Standard:
                    {
                        sp.blendDistancePositive.vector3Value = probeBlendDistancePositive;
                        sp.blendDistanceNegative.vector3Value = probeBlendDistanceNegative;

                        //save advanced/simplified saved data
                        if (sp.editorAdvancedModeEnabled.boolValue)
                        {
                            sp.editorAdvancedModeBlendDistancePositive.vector3Value = probeBlendDistancePositive;
                            sp.editorAdvancedModeBlendDistanceNegative.vector3Value = probeBlendDistanceNegative;
                        }
                        else
                        {
                            sp.editorSimplifiedModeBlendDistance.floatValue = probeBlendDistancePositive.x;
                        }
                        break;
                    }
                case InfluenceType.Normal:
                    {
                        sp.blendNormalDistancePositive.vector3Value = probeBlendDistancePositive;
                        sp.blendNormalDistanceNegative.vector3Value = probeBlendDistanceNegative;

                        //save advanced/simplified saved data
                        if (sp.editorAdvancedModeEnabled.boolValue)
                        {
                            sp.editorAdvancedModeBlendNormalDistancePositive.vector3Value = probeBlendDistancePositive;
                            sp.editorAdvancedModeBlendNormalDistanceNegative.vector3Value = probeBlendDistanceNegative;
                        }
                        else
                        {
                            sp.editorSimplifiedModeBlendNormalDistance.floatValue = probeBlendDistancePositive.x;
                        }
                        break;
                    }
            }
            sp.Apply();
        }

        static void Handle_InfluenceEditing(HDReflectionProbeUI s, SerializedHDReflectionProbe sp, Editor o)
        {
            var mat = Handles.matrix;
            var col = Handles.color;
            Handles.matrix = HDReflectionProbeEditorUtility.GetLocalSpace(sp.target);
            switch ((ShapeType)sp.influenceShape.enumValueIndex)
            {
                case ShapeType.Box:
                    {
                        s.alternativeBoxInfluenceHandle.center = sp.target.center;
                        s.alternativeBoxInfluenceHandle.size = sp.target.size;

                        EditorGUI.BeginChangeCheck();
                        s.alternativeBoxInfluenceHandle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(sp.target, "Modified Reflection Probe AABB");
                            Undo.RecordObject(sp.targetData, "Modified Reflection Probe AABB");

                            Vector3 center;
                            Vector3 size;
                            center = s.alternativeBoxInfluenceHandle.center;
                            size = s.alternativeBoxInfluenceHandle.size;

                            HDReflectionProbeEditorUtility.ValidateAABB(sp.target, ref center, ref size);

                            sp.target.center = center;
                            sp.target.size = size;

                            //ApplyConstraintsOnTargets(s, sp, o);

                            EditorUtility.SetDirty(sp.target);
                            EditorUtility.SetDirty(sp.targetData);
                        }
                        break;
                    }
                case ShapeType.Sphere:
                    {
                        s.sphereInfluenceHandle.center = sp.target.center;
                        s.sphereInfluenceHandle.radius = sp.targetData.influenceSphereRadius;

                        Handles.color = k_GizmoThemeColorExtent;
                        EditorGUI.BeginChangeCheck();
                        s.sphereInfluenceHandle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(sp.target, "Modified Reflection Probe AABB");
                            Undo.RecordObject(sp.targetData, "Modified Reflection Probe AABB");

                            var center = sp.target.center;
                            var influenceRadius = s.sphereInfluenceHandle.radius;
                            var radius = Vector3.one * influenceRadius;

                            HDReflectionProbeEditorUtility.ValidateAABB(sp.target, ref center, ref radius);
                            influenceRadius = radius.x;

                            sp.targetData.influenceSphereRadius = influenceRadius;

                            //ApplyConstraintsOnTargets(s, sp, o);

                            EditorUtility.SetDirty(sp.target);
                            EditorUtility.SetDirty(sp.targetData);
                        }
                        break;
                    }
            }
            Handles.matrix = mat;
            Handles.color = col;
        }

        static void Handle_OriginEditing(HDReflectionProbeUI s, SerializedHDReflectionProbe sp, Editor o)
        {
            var p = (ReflectionProbe)sp.so.targetObject;
            var transformPosition = p.transform.position;
            var size = p.size;

            EditorGUI.BeginChangeCheck();
            var newPostion = Handles.PositionHandle(transformPosition, HDReflectionProbeEditorUtility.GetLocalSpaceRotation(p));

            var changed = EditorGUI.EndChangeCheck();

            if (changed || s.oldLocalSpace != HDReflectionProbeEditorUtility.GetLocalSpace(p))
            {
                var localNewPosition = s.oldLocalSpace.inverse.MultiplyPoint3x4(newPostion);

                var b = new Bounds(p.center, size);
                localNewPosition = b.ClosestPoint(localNewPosition);

                Undo.RecordObject(p.transform, "Modified Reflection Probe Origin");
                p.transform.position = s.oldLocalSpace.MultiplyPoint3x4(localNewPosition);

                Undo.RecordObject(p, "Modified Reflection Probe Origin");
                p.center = HDReflectionProbeEditorUtility.GetLocalSpace(p).inverse.MultiplyPoint3x4(s.oldLocalSpace.MultiplyPoint3x4(p.center));

                EditorUtility.SetDirty(p);

                s.UpdateOldLocalSpace(p);
            }
        }

        [DrawGizmo(GizmoType.Active)]
        static void RenderGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            var e = GetEditorFor(reflectionProbe);
            if (e == null || !e.sceneViewEditing)
                return;

            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();

            switch (EditMode.editMode)
            {
                // Influence editing
                case EditMode.SceneViewEditMode.ReflectionProbeBox:
                    Gizmos_Influence(reflectionProbe, reflectionData, e, true);
                    break;
                // Influence fade editing
                case EditMode.SceneViewEditMode.GridBox:
                    Gizmos_InfluenceFade(reflectionProbe, reflectionData, e, InfluenceType.Standard, true);
                    break;
                // Influence normal fade editing
                case EditMode.SceneViewEditMode.Collider:
                    Gizmos_InfluenceFade(reflectionProbe, reflectionData, e, InfluenceType.Normal, true);
                    break;
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        static void DrawSelectedGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            var e = GetEditorFor(reflectionProbe);
            if (e == null || !e.sceneViewEditing)
                return;

            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();

            //Gizmos_Influence(reflectionProbe, reflectionData, e, false);
            Gizmos_InfluenceFade(reflectionProbe, reflectionData, null, InfluenceType.Standard, false);
            Gizmos_InfluenceFade(reflectionProbe, reflectionData, null, InfluenceType.Normal, false);

            DrawVerticalRay(reflectionProbe.transform);
            HDReflectionProbeEditorUtility.ChangeVisibility(reflectionProbe, true);
        }

        [DrawGizmo(GizmoType.NonSelected)]
        static void DrawNonSelectedGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            var e = GetEditorFor(reflectionProbe);
            if (e == null || !e.sceneViewEditing)
                return;

            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();
            if (reflectionData != null)
                HDReflectionProbeEditorUtility.ChangeVisibility(reflectionProbe, false);
        }

        static void Gizmos_InfluenceFade(ReflectionProbe p, HDAdditionalReflectionData a, HDReflectionProbeEditor e, InfluenceType type, bool isEdit)
        {
            var col = Gizmos.color;
            var mat = Gizmos.matrix;

            Gizmo6FacesBoxContained box;
            Vector3 boxCenterOffset;
            Vector3 boxSizeOffset;
            float sphereRadiusOffset;
            Color color;
            switch (type)
            {
                default:
                case InfluenceType.Standard:
                    {
                        box = e != null ? e.m_UIState.alternativeBoxBlendHandle : null;
                        boxCenterOffset = a.boxBlendCenterOffset;
                        boxSizeOffset = a.boxBlendSizeOffset;
                        sphereRadiusOffset = a.sphereBlendRadiusOffset;
                        color = isEdit ? k_GizmoThemeColorInfluenceBlendFace : k_GizmoThemeColorInfluenceBlend;
                        break;
                    }
                case InfluenceType.Normal:
                    {
                        box = e != null ? e.m_UIState.alternativeBoxBlendNormalHandle : null;
                        boxCenterOffset = a.boxBlendNormalCenterOffset;
                        boxSizeOffset = a.boxBlendNormalSizeOffset;
                        sphereRadiusOffset = a.sphereBlendNormalRadiusOffset;
                        color = isEdit ? k_GizmoThemeColorInfluenceNormalBlendFace : k_GizmoThemeColorInfluenceNormalBlend;
                        break;
                    }
            }

            Gizmos.matrix = HDReflectionProbeEditorUtility.GetLocalSpace(p);
            switch (a.influenceShape)
            {
                case ShapeType.Box:
                    {
                        Gizmos.color = color;
                        if(e != null) // e == null may occure when editor have still not been created at selection while the tool is not used for this part
                        {
                            box.DrawHull(isEdit);
                        }
                        else
                        {
                            if (isEdit)
                                Gizmos.DrawCube(p.center + boxCenterOffset, p.size + boxSizeOffset);
                            else
                                Gizmos.DrawWireCube(p.center + boxCenterOffset, p.size + boxSizeOffset);
                        }
                        break;
                    }
                case ShapeType.Sphere:
                    {
                        Gizmos.color = color;
                        if (isEdit)
                            Gizmos.DrawSphere(p.center, a.influenceSphereRadius + sphereRadiusOffset);
                        else
                            Gizmos.DrawWireSphere(p.center, a.influenceSphereRadius + sphereRadiusOffset);
                        break;
                    }
            }

            Gizmos.matrix = mat;
            Gizmos.color = col;
        }

        static void Gizmos_Influence(ReflectionProbe p, HDAdditionalReflectionData a, HDReflectionProbeEditor e, bool isEdit)
        {
            var col = Gizmos.color;
            var mat = Gizmos.matrix;

            Gizmos.matrix = HDReflectionProbeEditorUtility.GetLocalSpace(p);
            switch (a.influenceShape)
            {
                case ShapeType.Box:
                    {
                        Gizmos.color = k_GizmoThemeColorExtentFace;
                        e.m_UIState.alternativeBoxInfluenceHandle.DrawHull(isEdit);
                        break;
                    }
                case ShapeType.Sphere:
                    {
                        Gizmos.color = k_GizmoThemeColorExtentFace;
                        if (isEdit)
                            Gizmos.DrawSphere(p.center, a.influenceSphereRadius);
                        else
                            Gizmos.DrawWireSphere(p.center, a.influenceSphereRadius);
                        break;
                    }
            }

            Gizmos.matrix = mat;
            Gizmos.color = col;
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
    }
}
