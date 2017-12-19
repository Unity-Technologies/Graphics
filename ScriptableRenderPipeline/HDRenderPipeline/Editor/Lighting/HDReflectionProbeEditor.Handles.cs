using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDReflectionProbeEditor
    {
        internal static Color k_GizmoReflectionProbe = new Color(0xFF / 255f, 0xE5 / 255f, 0x94 / 255f, 0x20 / 255f);
        internal static Color k_GizmoReflectionProbeDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);
        internal static Color k_GizmoHandleReflectionProbe = new Color(0xFF / 255f, 0xE5 / 255f, 0xAA / 255f, 0xFF / 255f);

        void OnSceneGUI()
        {
            var s = m_UIState;
            var p = m_SerializedReflectionProbe;
            var o = this;

            if (!s.sceneViewEditing)
                return;

            EditorGUI.BeginChangeCheck();

            switch (EditMode.editMode)
            {
                case EditMode.SceneViewEditMode.ReflectionProbeBox:
                    if (p.influenceShape.enumValueIndex == 0)
                        Handle_InfluenceBoxEditing(s, p, o);
                    if (p.influenceShape.enumValueIndex == 1)
                        Handle_InfluenceSphereEditing(s, p, o);
                    break;
                case EditMode.SceneViewEditMode.GridBox:
                    if (p.influenceShape.enumValueIndex == 0)
                        Handle_ProjectionBoxEditing(s, p, o);
                    if (p.influenceShape.enumValueIndex == 1)
                        Handle_ProjectionSphereEditing(s, p, o);
                    break;
                case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                    Handle_OriginEditing(s, p, o);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
                Repaint();
        }

        static void Handle_InfluenceBoxEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            var p = (ReflectionProbe)sp.so.targetObject;
            var a = p.GetComponent<HDAdditionalReflectionData>();

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                s.boxInfluenceBoundsHandle.center = p.center;
                s.boxInfluenceBoundsHandle.size = p.size;
                s.boxBlendHandle.center = p.center;
                s.boxBlendHandle.size = p.size - Vector3.one * p.blendDistance * 2;

                EditorGUI.BeginChangeCheck();
                s.boxInfluenceBoundsHandle.DrawHandle();
                var influenceChanged = EditorGUI.EndChangeCheck();
                EditorGUI.BeginChangeCheck();
                s.boxBlendHandle.DrawHandle();
                if (influenceChanged || EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(p, "Modified Reflection Probe AABB");
                    var center = s.boxInfluenceBoundsHandle.center;
                    var influenceSize = s.boxInfluenceBoundsHandle.size;
                    var blendSize = s.boxBlendHandle.size;
                    ValidateAABB(p, ref center, ref influenceSize);
                    var blendDistance = influenceChanged
                        ? p.blendDistance
                        : ((influenceSize.x - blendSize.x) * 0.5f + (influenceSize.y - blendSize.y) * 0.5f + (influenceSize.z - blendSize.z) * 0.5f) / 3;
                    p.center = center;
                    p.size = influenceSize;
                    p.blendDistance = Mathf.Max(blendDistance, 0);
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
            var p = (ReflectionProbe)sp.so.targetObject;
            var reflectionData = p.GetComponent<HDAdditionalReflectionData>();

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                s.influenceSphereHandle.center = p.center;
                s.influenceSphereHandle.radius = reflectionData.influenceSphereRadius;
                s.sphereBlendHandle.center = p.center;
                s.sphereBlendHandle.radius = Mathf.Min(reflectionData.influenceSphereRadius - p.blendDistance * 2, reflectionData.influenceSphereRadius);

                EditorGUI.BeginChangeCheck();
                s.influenceSphereHandle.DrawHandle();
                var influenceChanged = EditorGUI.EndChangeCheck();
                EditorGUI.BeginChangeCheck();
                s.sphereBlendHandle.DrawHandle();
                if (influenceChanged || EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection influence volume");
                    var center = p.center;
                    var influenceRadius = s.influenceSphereHandle.radius;
                    var blendRadius = influenceChanged
                        ? Mathf.Max(influenceRadius - p.blendDistance * 2, 0)
                        : s.sphereBlendHandle.radius;

                    var radius = Vector3.one * influenceRadius;
                    
                    ValidateAABB(p, ref center, ref radius);
                    influenceRadius = radius.x;
                    var blendDistance = Mathf.Max(0, (influenceRadius - blendRadius) * 0.5f);

                    reflectionData.influenceSphereRadius = influenceRadius;
                    p.blendDistance = blendDistance;
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
            if (e == null)
                return;

            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();

            if (e.sceneViewEditing && EditMode.editMode == EditMode.SceneViewEditMode.ReflectionProbeBox)
            {
                var oldColor = Gizmos.color;
                Gizmos.color = k_GizmoReflectionProbe;

                Gizmos.matrix = GetLocalSpace(reflectionProbe);
                if (reflectionData.influenceShape == ReflectionInfluenceShape.Box)
                    Gizmos.DrawCube(reflectionProbe.center, -1f * reflectionProbe.size);
                if (reflectionData.influenceShape == ReflectionInfluenceShape.Sphere)
                    Gizmos.DrawSphere(reflectionProbe.center, reflectionData.influenceSphereRadius);
                    
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = oldColor;
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        static void DrawSelectedGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            Color oldColor = Gizmos.color;
            Gizmos.color = reflectionProbe.isActiveAndEnabled ? k_GizmoReflectionProbe : k_GizmoReflectionProbeDisabled;
            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();

            if (reflectionData.influenceShape == ReflectionInfluenceShape.Box)
            {
                DrawBoxInfluenceGizmo(reflectionProbe, oldColor);
            }
            if (reflectionData.influenceShape == ReflectionInfluenceShape.Sphere)
            {
                DrawSphereInfluenceGizmo(reflectionProbe, oldColor, reflectionData);
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
            Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.influenceSphereRadius);
            if (reflectionProbe.blendDistance > 0)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.influenceSphereRadius - 2 * reflectionProbe.blendDistance);
            }
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = oldColor;
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
