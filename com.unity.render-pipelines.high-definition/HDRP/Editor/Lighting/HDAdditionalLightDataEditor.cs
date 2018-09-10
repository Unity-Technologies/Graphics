using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HDAdditionalLightData))]
    class HDAdditionalLightDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }
    }

    public class HDAdditionalLightDataGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmoForHDAdditionalLightData(HDAdditionalLightData src, GizmoType gizmoType)
        {
            bool selected = (gizmoType & GizmoType.Selected) != 0;

            var light = src.gameObject.GetComponent<Light>();
            var gizmoColor = light.color;
            gizmoColor.a = selected ? 1.0f : 0.3f; // Fade for the gizmo
            Gizmos.color = Handles.color = gizmoColor;

            if (src.lightTypeExtent == LightTypeExtent.Punctual)
            {
                switch (light.type)
                {
                    case LightType.Directional:
                        CoreLightEditorUtilities.DrawDirectionalLightGizmo(light);
                        break;
                    case LightType.Point:
                        CoreLightEditorUtilities.DrawPointlightGizmo(light, selected);
                        break;
                    case LightType.Spot:
                        if (src.spotLightShape == SpotLightShape.Cone)
                            CoreLightEditorUtilities.DrawSpotlightGizmo(light, src.GetInnerSpotPercent01(), selected);
                        else if (src.spotLightShape == SpotLightShape.Pyramid)
                            HDLightEditorUtilities.DrawFrustumlightGizmo(light);
                        else if (src.spotLightShape == SpotLightShape.Box)
                            HDLightEditorUtilities.DrawFrustumlightGizmo(light);
                        break;
                }
            }
            else
            {
                switch (src.lightTypeExtent)
                {
                    case LightTypeExtent.Rectangle:
                        CoreLightEditorUtilities.DrawArealightGizmo(light);
                        break;
                    case LightTypeExtent.Line:
                        CoreLightEditorUtilities.DrawArealightGizmo(light);
                        break;
                }
            }

            if (selected)
            {
                // Trace a ray down to better locate the light location
                Ray ray = new Ray(src.gameObject.transform.position, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Handles.color = Color.green;
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                    Handles.DrawLine(src.gameObject.transform.position, hit.point);
                    Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);

                    Handles.color = Color.red;
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                    Handles.DrawLine(src.gameObject.transform.position, hit.point);
                    Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);
                }
            }
        }
    }
}
