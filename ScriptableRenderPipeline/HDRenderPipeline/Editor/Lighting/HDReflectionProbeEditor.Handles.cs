using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDReflectionProbeEditor
    {
        void OnSceneGUI()
        {
            serializedObject.Update();
            m_AdditionalDataSerializedObject.Update();

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
                        DoInfluenceBoxEditing(s, p, o);
                    if (p.influenceShape.enumValueIndex == 1)
                        DoInfluenceSphereEditing(s, p, o);
                    break;
                case EditMode.SceneViewEditMode.GridBox:
                    if (p.influenceShape.enumValueIndex == 0)
                        DoProjectionBoxEditing(s, p, o);
                    if (p.influenceShape.enumValueIndex == 1)
                        DoProjectionSphereEditing(s, p, o);
                    break;
                case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                    DoOriginEditing(s, p, o);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
                Repaint();

            serializedObject.ApplyModifiedProperties();
            m_AdditionalDataSerializedObject.ApplyModifiedProperties();
        }

        static void DoInfluenceBoxEditing(UIState s, SerializedReflectionProbe sp, Editor o)
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

        static void DoProjectionBoxEditing(UIState s, SerializedReflectionProbe sp, Editor o)
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

        static void DoInfluenceSphereEditing(UIState s, SerializedReflectionProbe sp, Editor o)
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

        static void DoProjectionSphereEditing(UIState s, SerializedReflectionProbe sp, Editor o)
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

        static void DoOriginEditing(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            var p = (ReflectionProbe)sp.so.targetObject;
            var transformPosition = p.transform.position;
            var size = p.size;

            EditorGUI.BeginChangeCheck();
            var newPostion = Handles.PositionHandle(transformPosition, GetLocalSpaceRotation(p));

            var changed = EditorGUI.EndChangeCheck();

            if (changed || s.oldLocalSpace != GetLocalSpace(p))
            {
                Vector3 localNewPosition = s.oldLocalSpace.inverse.MultiplyPoint3x4(newPostion);

                Bounds b = new Bounds(p.center, size);
                localNewPosition = b.ClosestPoint(localNewPosition);

                Undo.RecordObject(p.transform, "Modified Reflection Probe Origin");
                p.transform.position = s.oldLocalSpace.MultiplyPoint3x4(localNewPosition);

                Undo.RecordObject(p, "Modified Reflection Probe Origin");
                p.center = GetLocalSpace(p).inverse.MultiplyPoint3x4(s.oldLocalSpace.MultiplyPoint3x4(p.center));

                EditorUtility.SetDirty(p);

                s.AddOperation(Operation.UpdateOldLocalSpace);
            }
        }
    }
}
