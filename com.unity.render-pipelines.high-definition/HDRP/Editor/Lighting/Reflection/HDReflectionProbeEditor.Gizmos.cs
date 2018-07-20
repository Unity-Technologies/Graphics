using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDReflectionProbeEditor
    {
        static Mesh sphere;
        static Material material;

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
            if (e == null)
                return;

            var reflectionData = reflectionProbe.GetComponent<HDAdditionalReflectionData>();
            Gizmos_CapturePoint(reflectionProbe, reflectionData, e);

            if (!e.sceneViewEditing)
                return;

            //Gizmos_Influence(reflectionProbe, reflectionData, e, false);
            Gizmos_InfluenceFade(reflectionProbe, reflectionData, null, InfluenceType.Standard, false);
            Gizmos_InfluenceFade(reflectionProbe, reflectionData, null, InfluenceType.Normal, false);

            DrawVerticalRay(reflectionProbe.transform);
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
                        if (e != null) // e == null may occure when editor have still not been created at selection while the tool is not used for this part
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

        static void Gizmos_CapturePoint(ReflectionProbe p, HDAdditionalReflectionData a, HDReflectionProbeEditor e)
        {
            if(sphere == null)
            {
                sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            }
            if(material == null)
            {
                material = new Material(Shader.Find("Debug/ReflectionProbePreview"));
            }
            material.SetTexture("_Cubemap", p.texture);
            material.SetPass(0);
            Graphics.DrawMeshNow(sphere, Matrix4x4.TRS(p.transform.position, Quaternion.identity, Vector3.one));
        }
    }
}
