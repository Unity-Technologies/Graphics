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
        internal static Color k_GizmoInfluenceVolume = new Color(0xFF / 255f, 0xE5 / 255f, 0x94 / 255f, 0x20 / 255f);
        internal static Color k_GizmoInfluenceNormalVolume = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        internal static Color k_GizmoReflectionProbeDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);
        internal static Color k_GizmoHandleReflectionProbe = new Color(0xFF / 255f, 0xE5 / 255f, 0xAA / 255f, 0xFF / 255f);

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
            var blendDistance = sp.target.blendDistance;
            Handle_InfluenceBoxEditing_Internal(s, sp, o, s.boxBlendHandle, k_GizmoInfluenceVolume, ref blendDistance);
            sp.target.blendDistance = blendDistance;
        }

        static void Handle_InfluenceNormalBoxEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            Handle_InfluenceBoxEditing_Internal(s, sp, o, s.boxBlendHandle, k_GizmoInfluenceNormalVolume, ref sp.targetData.blendNormalDistance);
        }

        static void Handle_InfluenceBoxEditing_Internal(UIState s, SerializedReflectionProbe sp, Editor o, BoxBoundsHandle blendBox, Color blendHandleColor, ref float probeBlendDistance)
        {
            var p = (ReflectionProbe)sp.so.targetObject;

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                s.boxInfluenceBoundsHandle.center = p.center;
                s.boxInfluenceBoundsHandle.size = p.size;
                blendBox.center = p.center;
                blendBox.size = p.size - Vector3.one * probeBlendDistance * 2;

                EditorGUI.BeginChangeCheck();
                s.boxInfluenceBoundsHandle.DrawHandle();
                var influenceChanged = EditorGUI.EndChangeCheck();
                EditorGUI.BeginChangeCheck();
                Handles.color = blendHandleColor;
                blendBox.DrawHandle();
                if (influenceChanged || EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(p, "Modified Reflection Probe AABB");
                    var center = s.boxInfluenceBoundsHandle.center;
                    var influenceSize = s.boxInfluenceBoundsHandle.size;
                    var blendSize = blendBox.size;
                    ValidateAABB(p, ref center, ref influenceSize);
                    var blendDistance = influenceChanged
                        ? probeBlendDistance
                        : ((influenceSize.x - blendSize.x) * 0.5f + (influenceSize.y - blendSize.y) * 0.5f + (influenceSize.z - blendSize.z) * 0.5f) / 3;
                    p.center = center;
                    p.size = influenceSize;
                    probeBlendDistance = Mathf.Max(blendDistance, 0);
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
                s.boxProjectionBoundsHandle.center = reflectionData.boxReprojectionVolumeCenter;
                s.boxProjectionBoundsHandle.size = reflectionData.boxReprojectionVolumeSize;

                EditorGUI.BeginChangeCheck();
                s.boxProjectionBoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection Probe AABB");
                    var center = s.boxProjectionBoundsHandle.center;
                    var size = s.boxProjectionBoundsHandle.size;
                    ValidateAABB(p, ref center, ref size);
                    reflectionData.boxReprojectionVolumeCenter = center;
                    reflectionData.boxReprojectionVolumeSize = size;
                    EditorUtility.SetDirty(reflectionData);
                }
            }
        }

        static void Handle_InfluenceSphereEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            var blendDistance = sp.target.blendDistance;
            Handle_InfluenceSphereEditing_Internal(s, sp, o, s.sphereBlendHandle, ref blendDistance);
            sp.target.blendDistance = blendDistance;
        }

        static void Handle_InfluenceNormalSphereEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            Handle_InfluenceSphereEditing_Internal(s, sp, o, s.sphereBlendHandle, ref sp.targetData.blendNormalDistance);
        }

        static void Handle_InfluenceSphereEditing_Internal(UIState s, SerializedReflectionProbe sp, Editor o, SphereBoundsHandle sphereBlend, ref float probeBlendDistance)
        {
            var p = (ReflectionProbe)sp.so.targetObject;
            var reflectionData = p.GetComponent<HDAdditionalReflectionData>();

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                s.influenceSphereHandle.center = p.center;
                s.influenceSphereHandle.radius = reflectionData.influenceSphereRadius;
                sphereBlend.center = p.center;
                sphereBlend.radius = Mathf.Min(reflectionData.influenceSphereRadius - probeBlendDistance * 2, reflectionData.influenceSphereRadius);

                EditorGUI.BeginChangeCheck();
                s.influenceSphereHandle.DrawHandle();
                var influenceChanged = EditorGUI.EndChangeCheck();
                EditorGUI.BeginChangeCheck();
                sphereBlend.DrawHandle();
                if (influenceChanged || EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection influence volume");
                    var center = p.center;
                    var influenceRadius = s.influenceSphereHandle.radius;
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
                s.projectionSphereHandle.center = p.center;
                s.projectionSphereHandle.radius = reflectionData.sphereReprojectionVolumeRadius;

                EditorGUI.BeginChangeCheck();
                s.projectionSphereHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection Probe projection volume");
                    var center = s.projectionSphereHandle.center;
                    var radius = s.projectionSphereHandle.radius;
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
                    Gizmos.color = k_GizmoInfluenceVolume;

                    Gizmos.matrix = GetLocalSpace(reflectionProbe);
                    if (reflectionData.influenceShape == ReflectionInfluenceShape.Box)
                        Gizmos.DrawCube(reflectionProbe.center, -1f * reflectionProbe.size);
                    if (reflectionData.influenceShape == ReflectionInfluenceShape.Sphere)
                        Gizmos.DrawSphere(reflectionProbe.center, reflectionData.influenceSphereRadius);

                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.color = oldColor;
                        break;
                }
                case EditMode.SceneViewEditMode.Collider:
                {
                    var oldColor = Gizmos.color;
                    Gizmos.color = k_GizmoInfluenceNormalVolume;

                    Gizmos.matrix = GetLocalSpace(reflectionProbe);
                    if (reflectionData.influenceShape == ReflectionInfluenceShape.Box)
                        Gizmos.DrawCube(reflectionProbe.center, -1f * reflectionProbe.size + Vector3.one * 2f * reflectionData.blendNormalDistance);
                    if (reflectionData.influenceShape == ReflectionInfluenceShape.Sphere)
                        Gizmos.DrawSphere(reflectionProbe.center, reflectionData.influenceSphereRadius - reflectionData.blendNormalDistance * 2f);

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
            var influenceColor = reflectionProbe.isActiveAndEnabled ? k_GizmoInfluenceVolume : k_GizmoReflectionProbeDisabled;
            var influenceNormalColor = reflectionProbe.isActiveAndEnabled ? k_GizmoInfluenceNormalVolume : k_GizmoReflectionProbeDisabled;
            Gizmos.color = influenceColor;
            Gizmos.matrix = GetLocalSpace(reflectionProbe);
            Gizmos.DrawWireCube(reflectionProbe.center, reflectionProbe.size);
            if (reflectionProbe.blendDistance > 0)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireCube(reflectionProbe.center, GetBlendBoxSize(reflectionProbe, reflectionProbe.blendDistance));
            }
            if (reflectionData.blendNormalDistance > 0)
            {
                Gizmos.color = new Color(influenceNormalColor.r, influenceNormalColor.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireCube(reflectionProbe.center, GetBlendBoxSize(reflectionProbe, reflectionData.blendNormalDistance));
            }
            Gizmos.matrix = Matrix4x4.identity;
        }

        static Vector3 GetBlendBoxSize(ReflectionProbe reflectionProbe, float blendDistance)
        {
            return new Vector3(reflectionProbe.size.x - blendDistance * 2, reflectionProbe.size.y - blendDistance * 2, reflectionProbe.size.z - blendDistance * 2);
        }

        static void DrawSphereInfluenceGizmo(ReflectionProbe reflectionProbe, HDAdditionalReflectionData reflectionData)
        {
            var influenceColor = reflectionProbe.isActiveAndEnabled ? k_GizmoInfluenceVolume : k_GizmoReflectionProbeDisabled;
            var influenceNormalColor = reflectionProbe.isActiveAndEnabled ? k_GizmoInfluenceNormalVolume : k_GizmoReflectionProbeDisabled;
            Gizmos.color = influenceColor;
            Gizmos.matrix = GetLocalSpace(reflectionProbe);
            Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.influenceSphereRadius);
            if (reflectionProbe.blendDistance > 0)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.influenceSphereRadius - 2 * reflectionProbe.blendDistance);
            }
            if (reflectionData.blendNormalDistance > 0)
            {
                Gizmos.color = new Color(influenceNormalColor.r, influenceNormalColor.g, influenceNormalColor.b, 0.3f);
                Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.influenceSphereRadius - 2 * reflectionData.blendNormalDistance);
            }
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
