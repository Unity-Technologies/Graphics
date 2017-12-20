using System;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDReflectionProbeEditor
    {
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

        void OnSceneGUI()
        {
            var s = m_UIState;
            var p = m_SerializedReflectionProbe;
            var o = this;

            BakeRealtimeProbeIfPositionChanged(s, p, o);

            if (!s.sceneViewEditing)
                return;

            EditorGUI.BeginChangeCheck();

            switch (EditMode.editMode)
            {
                case EditMode.SceneViewEditMode.ReflectionProbeBox:
                {
                    if (p.influenceShape.enumValueIndex == 0)
                        Handle_InfluenceBoxEditing(s, p, o);
                    if (p.influenceShape.enumValueIndex == 1)
                        Handle_InfluenceSphereEditing(s, p, o);
                    break;
                }
                case EditMode.SceneViewEditMode.GridBox:
                {
                    if (p.influenceShape.enumValueIndex == 0)
                        Handle_ProjectionBoxEditing(s, p, o);
                    if (p.influenceShape.enumValueIndex == 1)
                        Handle_ProjectionSphereEditing(s, p, o);
                    break;
                }
                case EditMode.SceneViewEditMode.Collider:
                {
                    if (p.influenceShape.enumValueIndex == 0)
                        Handle_InfluenceNormalBoxEditing(s, p, o);
                    if (p.influenceShape.enumValueIndex == 1)
                        Handle_InfluenceNormalSphereEditing(s, p, o);
                    break;
                }
                case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                    Handle_OriginEditing(s, p, o);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
                Repaint();
        }

        void BakeRealtimeProbeIfPositionChanged(UIState s, SerializedReflectionProbe sp, Editor o)
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

        static void Handle_InfluenceBoxEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            Handle_InfluenceBoxEditing_Internal(
                s, sp, o, 
                s.boxBlendHandle, k_GizmoThemeColorInfluenceBlend, 
                ref sp.targetData.blendDistance, ref sp.targetData.blendDistance2);
        }

        static void Handle_InfluenceNormalBoxEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            Handle_InfluenceBoxEditing_Internal(
                s, sp, o, 
                s.boxBlendHandle, k_GizmoThemeColorInfluenceNormalBlend,
                ref sp.targetData.blendNormalDistance, ref sp.targetData.blendNormalDistance2);
        }

        static void Handle_InfluenceBoxEditing_Internal(
            UIState s, SerializedReflectionProbe sp, Editor o, 
            BoxBoundsHandle blendBox, 
            Color blendHandleColor,
            ref Vector3 probeBlendDistancePositive,
            ref Vector3 probeBlendDistanceNegative)
        {
            var p = sp.target;

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                s.boxExtentHandle.center = p.center;
                s.boxExtentHandle.size = p.size;
                blendBox.center = p.center - (probeBlendDistancePositive - probeBlendDistanceNegative) * 0.5f;
                blendBox.size = p.size - probeBlendDistancePositive - probeBlendDistanceNegative;

                Handles.color = k_GizmoThemeColorExtent;
                EditorGUI.BeginChangeCheck();
                s.boxExtentHandle.DrawHandle();
                var extentChanged = EditorGUI.EndChangeCheck();
                EditorGUI.BeginChangeCheck();
                Handles.color = blendHandleColor;
                blendBox.DrawHandle();
                if (extentChanged || EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(p, "Modified Reflection Probe AABB");
                    var center = s.boxExtentHandle.center;
                    var extents = s.boxExtentHandle.size;
                    ValidateAABB(p, ref center, ref extents);

                    Vector3 blendDistancePositive, blendDistanceNegative;

                    if (extentChanged)
                    {
                        blendDistancePositive = Vector3.Min(probeBlendDistancePositive, extents);
                        blendDistanceNegative = Vector3.Min(probeBlendDistanceNegative, extents);
                    }
                    else
                    {
                        var diff = 2 * (blendBox.center - center);
                        var sum = extents - blendBox.size;
                        var positive = (sum - diff) * 0.5f;
                        var negative = (sum + diff) * 0.5f;
                        blendDistancePositive = Vector3.Max(Vector3.zero, Vector3.Min(positive, extents));
                        blendDistanceNegative = Vector3.Max(Vector3.zero, Vector3.Min(negative, extents));
                    }
                    
                    p.center = center;
                    p.size = extents;
                    probeBlendDistancePositive = blendDistancePositive;
                    probeBlendDistanceNegative = blendDistanceNegative;
                    EditorUtility.SetDirty(p);
                }
            }
        }

        static void Handle_ProjectionBoxEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            var p = (ReflectionProbe)sp.so.targetObject;
            var reflectionData = p.GetComponent<HDAdditionalReflectionData>();

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                s.boxProjectionHandle.center = reflectionData.boxReprojectionVolumeCenter;
                s.boxProjectionHandle.size = reflectionData.boxReprojectionVolumeSize;

                EditorGUI.BeginChangeCheck();
                s.boxProjectionHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection Probe AABB");
                    var center = s.boxProjectionHandle.center;
                    var size = s.boxProjectionHandle.size;
                    ValidateAABB(p, ref center, ref size);
                    reflectionData.boxReprojectionVolumeCenter = center;
                    reflectionData.boxReprojectionVolumeSize = size;
                    EditorUtility.SetDirty(reflectionData);
                }
            }
        }

        static void Handle_InfluenceSphereEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            var blendDistance = sp.targetData.blendDistance.x;
            Handle_InfluenceSphereEditing_Internal(s, sp, o, s.sphereBlendHandle, k_GizmoThemeColorInfluenceBlend, ref blendDistance);
            sp.targetData.blendDistance.x = blendDistance;
        }

        static void Handle_InfluenceNormalSphereEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            var blendDistance = sp.targetData.blendNormalDistance.x;
            Handle_InfluenceSphereEditing_Internal(s, sp, o, s.sphereBlendHandle, k_GizmoThemeColorInfluenceNormalBlend, ref blendDistance);
            sp.targetData.blendNormalDistance.x = blendDistance;
        }

        static void Handle_InfluenceSphereEditing_Internal(
            UIState s, SerializedReflectionProbe sp, Editor o, 
            SphereBoundsHandle sphereBlend, 
            Color blendHandleColor,
            ref float probeBlendDistance)
        {
            var p = (ReflectionProbe)sp.so.targetObject;
            var reflectionData = p.GetComponent<HDAdditionalReflectionData>();

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                s.sphereExtentHandle.center = p.center;
                s.sphereExtentHandle.radius = reflectionData.influenceSphereRadius;
                sphereBlend.center = p.center;
                sphereBlend.radius = Mathf.Min(reflectionData.influenceSphereRadius - probeBlendDistance * 2, reflectionData.influenceSphereRadius);

                Handles.color = k_GizmoThemeColorExtent;
                EditorGUI.BeginChangeCheck();
                s.sphereExtentHandle.DrawHandle();
                var influenceChanged = EditorGUI.EndChangeCheck();
                EditorGUI.BeginChangeCheck();
                Handles.color = blendHandleColor;
                sphereBlend.DrawHandle();
                if (influenceChanged || EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection influence volume");
                    var center = p.center;
                    var influenceRadius = s.sphereExtentHandle.radius;
                    var blendRadius = influenceChanged
                        ? Mathf.Max(influenceRadius - probeBlendDistance * 2, 0)
                        : sphereBlend.radius;

                    var radius = Vector3.one * influenceRadius;

                    ValidateAABB(p, ref center, ref radius);
                    influenceRadius = radius.x;
                    var blendDistance = Mathf.Max(0, (influenceRadius - blendRadius) * 0.5f);

                    reflectionData.influenceSphereRadius = influenceRadius;
                    probeBlendDistance = blendDistance;
                    EditorUtility.SetDirty(p);
                    EditorUtility.SetDirty(reflectionData);
                }
            }
        }

        static void Handle_ProjectionSphereEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            var p = (ReflectionProbe)sp.so.targetObject;
            var reflectionData = p.GetComponent<HDAdditionalReflectionData>();

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                s.sphereProjectionHandle.center = p.center;
                s.sphereProjectionHandle.radius = reflectionData.sphereReprojectionVolumeRadius;

                EditorGUI.BeginChangeCheck();
                s.sphereProjectionHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection Probe projection volume");
                    var center = s.sphereProjectionHandle.center;
                    var radius = s.sphereProjectionHandle.radius;
                    //ValidateAABB(ref center, ref radius);
                    reflectionData.sphereReprojectionVolumeRadius = radius;
                    EditorUtility.SetDirty(reflectionData);
                }
            }
        }

        static void Handle_OriginEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            var p = (ReflectionProbe)sp.so.targetObject;
            var transformPosition = p.transform.position;
            var size = p.size;

            EditorGUI.BeginChangeCheck();
            var newPostion = Handles.PositionHandle(transformPosition, GetLocalSpaceRotation(p));

            var changed = EditorGUI.EndChangeCheck();

            if (changed || s.oldLocalSpace != GetLocalSpace(p))
            {
                var localNewPosition = s.oldLocalSpace.inverse.MultiplyPoint3x4(newPostion);

                var b = new Bounds(p.center, size);
                localNewPosition = b.ClosestPoint(localNewPosition);

                Undo.RecordObject(p.transform, "Modified Reflection Probe Origin");
                p.transform.position = s.oldLocalSpace.MultiplyPoint3x4(localNewPosition);

                Undo.RecordObject(p, "Modified Reflection Probe Origin");
                p.center = GetLocalSpace(p).inverse.MultiplyPoint3x4(s.oldLocalSpace.MultiplyPoint3x4(p.center));

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
                case EditMode.SceneViewEditMode.ReflectionProbeBox:
                {
                    var oldColor = Gizmos.color;

                    Gizmos.matrix = GetLocalSpace(reflectionProbe);
                    if (reflectionData.influenceShape == ReflectionInfluenceShape.Box)
                    {
                        Gizmos.color = k_GizmoThemeColorExtentFace;
                        Gizmos.DrawCube(reflectionProbe.center, reflectionProbe.size);
                        Gizmos.color = k_GizmoThemeColorInfluenceBlendFace;
                        Gizmos.DrawCube(reflectionProbe.center + reflectionData.boxBlendCenterOffset, reflectionProbe.size + reflectionData.boxBlendSizeOffset);
                    }
                    if (reflectionData.influenceShape == ReflectionInfluenceShape.Sphere)
                    {
                        Gizmos.color = k_GizmoThemeColorExtentFace;
                        Gizmos.DrawSphere(reflectionProbe.center, reflectionData.influenceSphereRadius);
                        Gizmos.color = k_GizmoThemeColorInfluenceBlendFace;
                        Gizmos.DrawSphere(reflectionProbe.center, reflectionData.influenceSphereRadius + reflectionData.sphereBlendRadiusOffset);
                    }

                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.color = oldColor;
                    break;
                }
                case EditMode.SceneViewEditMode.Collider:
                {
                    var oldColor = Gizmos.color;
                    Gizmos.color = k_GizmoThemeColorInfluenceNormalBlendFace;

                    Gizmos.matrix = GetLocalSpace(reflectionProbe);
                    if (reflectionData.influenceShape == ReflectionInfluenceShape.Box)
                        Gizmos.DrawCube(reflectionProbe.center + reflectionData.boxBlendNormalCenterOffset, reflectionProbe.size + reflectionData.boxBlendNormalSizeOffset);
                    if (reflectionData.influenceShape == ReflectionInfluenceShape.Sphere)
                        Gizmos.DrawSphere(reflectionProbe.center, reflectionData.influenceSphereRadius + reflectionData.sphereBlendNormalRadiusOffset);

                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.color = oldColor;
                        break;
                }
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        static void DrawSelectedGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            var oldColor = Gizmos.color;
            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();

            if (reflectionData.influenceShape == ReflectionInfluenceShape.Box)
            {
                DrawBoxInfluenceGizmo(reflectionProbe, reflectionData);
            }
            if (reflectionData.influenceShape == ReflectionInfluenceShape.Sphere)
            {
                DrawSphereInfluenceGizmo(reflectionProbe, reflectionData);
            }
            if (reflectionData.useSeparateProjectionVolume)
            {
                DrawReprojectionVolumeGizmo(reflectionProbe, reflectionData);
            }
            Gizmos.color = oldColor;

            DrawVerticalRay(reflectionProbe.transform);

            ChangeVisibility(reflectionProbe, true);
        }

        [DrawGizmo(GizmoType.NonSelected)]
        static void DrawNonSelectedGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();
            if (reflectionData != null)
                ChangeVisibility(reflectionProbe, false);
        }

        static void DrawBoxInfluenceGizmo(ReflectionProbe reflectionProbe, HDAdditionalReflectionData reflectionData)
        {
            Gizmos.color = reflectionProbe.isActiveAndEnabled ? k_GizmoThemeColorExtent : k_GizmoThemeColorDisabled;
            Gizmos.matrix = GetLocalSpace(reflectionProbe);
            Gizmos.DrawWireCube(reflectionProbe.center, reflectionProbe.size);

            Gizmos.color = reflectionProbe.isActiveAndEnabled ? k_GizmoThemeColorInfluenceBlend : k_GizmoThemeColorDisabled;
            Gizmos.DrawWireCube(reflectionProbe.center + reflectionData.boxBlendCenterOffset, reflectionProbe.size + reflectionData.boxBlendSizeOffset);

            Gizmos.color = reflectionProbe.isActiveAndEnabled ? k_GizmoThemeColorInfluenceNormalBlend : k_GizmoThemeColorDisabled;
            Gizmos.DrawWireCube(reflectionProbe.center + reflectionData.boxBlendNormalCenterOffset, reflectionProbe.size + reflectionData.boxBlendNormalSizeOffset);

            Gizmos.matrix = Matrix4x4.identity;
        }

        static void DrawSphereInfluenceGizmo(ReflectionProbe reflectionProbe, HDAdditionalReflectionData reflectionData)
        {
            Gizmos.color = reflectionProbe.isActiveAndEnabled ? k_GizmoThemeColorExtent : k_GizmoThemeColorDisabled;
            Gizmos.matrix = GetLocalSpace(reflectionProbe);
            Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.influenceSphereRadius);

            Gizmos.color = reflectionProbe.isActiveAndEnabled ? k_GizmoThemeColorInfluenceBlend : k_GizmoThemeColorDisabled;
            Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.influenceSphereRadius + reflectionData.sphereBlendRadiusOffset);

            Gizmos.color = reflectionProbe.isActiveAndEnabled ? k_GizmoThemeColorInfluenceNormalBlend : k_GizmoThemeColorDisabled;
            Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.influenceSphereRadius + reflectionData.sphereBlendNormalRadiusOffset);

            Gizmos.matrix = Matrix4x4.identity;
        }

        static void DrawReprojectionVolumeGizmo(ReflectionProbe reflectionProbe, HDAdditionalReflectionData reflectionData)
        {
            Color reprojectionColor = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.3f);
            Gizmos.color = reprojectionColor;
            Gizmos.matrix = GetLocalSpace(reflectionProbe);
            if (reflectionData.influenceShape == ReflectionInfluenceShape.Box)
            {
                Gizmos.DrawWireCube(reflectionData.boxReprojectionVolumeCenter, reflectionData.boxReprojectionVolumeSize);
            }
            if (reflectionData.influenceShape == ReflectionInfluenceShape.Sphere)
            {
                Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.sphereReprojectionVolumeRadius);
            }
            Gizmos.matrix = Matrix4x4.identity;
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
