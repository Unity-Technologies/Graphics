using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDReflectionProbeEditor
    {
        enum InfluenceType
        {
            Standard,
            Normal
        }

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
    }
}
