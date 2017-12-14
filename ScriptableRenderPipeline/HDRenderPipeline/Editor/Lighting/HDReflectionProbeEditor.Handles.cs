using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDReflectionProbeEditor
    {
        internal static Color kGizmoReflectionProbe = new Color(0xFF / 255f, 0xE5 / 255f, 0x94 / 255f, 0x80 / 255f);
        internal static Color kGizmoReflectionProbeDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x60 / 255f);
        internal static Color kGizmoHandleReflectionProbe = new Color(0xFF / 255f, 0xE5 / 255f, 0xAA / 255f, 0xFF / 255f);

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

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                s.boxInfluenceBoundsHandle.center = p.center;
                s.boxInfluenceBoundsHandle.size = p.size;
                s.boxBlendHandle.center = p.center;
                s.boxBlendHandle.size = p.size - Vector3.one * p.blendDistance * 2;

                EditorGUI.BeginChangeCheck();
                s.boxInfluenceBoundsHandle.DrawHandle();
                s.boxBlendHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(p, "Modified Reflection Probe AABB");
                    var center = s.boxInfluenceBoundsHandle.center;
                    var size = s.boxInfluenceBoundsHandle.size;
                    var blendDistance = ((p.size.x - s.boxBlendHandle.size.x) / 2 + (p.size.y - s.boxBlendHandle.size.y) / 2 + (p.size.z - s.boxBlendHandle.size.z) / 2) / 3;
                    ValidateAABB(p, ref center, ref size);
                    p.center = center;
                    p.size = size;
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
                s.boxProjectionBoundsHandle.center = reflectionData.m_BoxReprojectionVolumeCenter;
                s.boxProjectionBoundsHandle.size = reflectionData.m_BoxReprojectionVolumeSize;

                EditorGUI.BeginChangeCheck();
                s.boxProjectionBoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection Probe AABB");
                    var center = s.boxProjectionBoundsHandle.center;
                    var size = s.boxProjectionBoundsHandle.size;
                    ValidateAABB(p, ref center, ref size);
                    reflectionData.m_BoxReprojectionVolumeCenter = center;
                    reflectionData.m_BoxReprojectionVolumeSize = size;
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
                s.influenceSphereHandle.radius = reflectionData.m_InfluenceSphereRadius;
                s.sphereBlendHandle.center = p.center;
                s.sphereBlendHandle.radius = Mathf.Min(reflectionData.m_InfluenceSphereRadius - p.blendDistance * 2, reflectionData.m_InfluenceSphereRadius);

                EditorGUI.BeginChangeCheck();
                s.influenceSphereHandle.DrawHandle();
                s.sphereBlendHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection influence volume");
                    var center = s.influenceSphereHandle.center;
                    var radius = new Vector3(s.influenceSphereHandle.radius, s.influenceSphereHandle.radius, s.influenceSphereHandle.radius);
                    var blendDistance = (s.influenceSphereHandle.radius - s.sphereBlendHandle.radius) / 2;
                    ValidateAABB(p, ref center, ref radius);
                    reflectionData.m_InfluenceSphereRadius = radius.x;
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
                s.projectionSphereHandle.radius = reflectionData.m_SphereReprojectionVolumeRadius;

                EditorGUI.BeginChangeCheck();
                s.projectionSphereHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(reflectionData, "Modified Reflection Probe projection volume");
                    var center = s.projectionSphereHandle.center;
                    var radius = s.projectionSphereHandle.radius;
                    //ValidateAABB(ref center, ref radius);
                    reflectionData.m_SphereReprojectionVolumeRadius = radius;
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
                Gizmos.color = kGizmoReflectionProbe;

                Gizmos.matrix = GetLocalSpace(reflectionProbe);
                if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Box)
                    Gizmos.DrawCube(reflectionProbe.center, -1f * reflectionProbe.size);
                if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Sphere)
                    Gizmos.DrawSphere(reflectionProbe.center, reflectionData.m_InfluenceSphereRadius);
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = oldColor;
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        static void DrawSelectedGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            Color oldColor = Gizmos.color;
            Gizmos.color = reflectionProbe.isActiveAndEnabled ? kGizmoReflectionProbe : kGizmoReflectionProbeDisabled;
            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();

            if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Box)
            {
                DrawBoxInfluenceGizmo(reflectionProbe, oldColor);
            }
            if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Sphere)
            {
                DrawSphereInfluenceGizmo(reflectionProbe, oldColor, reflectionData);
            }
            if (reflectionData.m_UseSeparateProjectionVolume)
            {
                DrawReprojectionVolumeGizmo(reflectionProbe, reflectionData);
            }
            Gizmos.color = oldColor;

            DrawVerticalRay(reflectionProbe.transform);

            reflectionData.ChangeVisibility(true);
        }

        [DrawGizmo(GizmoType.NonSelected)]
        static void DrawNonSelectedGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();
            if (reflectionData != null)
                reflectionData.ChangeVisibility(false);
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
            Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.m_InfluenceSphereRadius);
            if (reflectionProbe.blendDistance > 0)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.m_InfluenceSphereRadius - 2 * reflectionProbe.blendDistance);
            }
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = oldColor;
        }

        static void DrawReprojectionVolumeGizmo(ReflectionProbe reflectionProbe, HDAdditionalReflectionData reflectionData)
        {
            Color reprojectionColor = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.3f);
            Gizmos.color = reprojectionColor;
            Gizmos.matrix = GetLocalSpace(reflectionProbe);
            if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Box)
            {
                Gizmos.DrawWireCube(reflectionData.m_BoxReprojectionVolumeCenter, reflectionData.m_BoxReprojectionVolumeSize);
            }
            if (reflectionData.m_InfluenceShape == ReflectionInfluenceShape.Sphere)
            {
                Gizmos.DrawWireSphere(reflectionProbe.center, reflectionData.m_SphereReprojectionVolumeRadius);
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
