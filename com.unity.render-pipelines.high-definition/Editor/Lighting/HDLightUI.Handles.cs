using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDLightUI
    {
        public static void DrawHandles(HDAdditionalLightData additionalData, Editor owner)
        {
            Light light = additionalData.legacyLight;

            Color wireframeColorAbove = (owner as HDLightEditor).legacyLightColor;
            Color handleColorAbove = CoreLightEditorUtilities.GetLightHandleColor(wireframeColorAbove);
            Color wireframeColorBehind = CoreLightEditorUtilities.GetLightBehindObjectWireframeColor(wireframeColorAbove);
            Color handleColorBehind = CoreLightEditorUtilities.GetLightHandleColor(wireframeColorBehind);

            switch (additionalData.type)
            {
                case HDLightType.Directional:
                case HDLightType.Point:
                    //use legacy handles for those cases:
                    //See HDLightEditor
                    break;
                case HDLightType.Spot:
                    switch (additionalData.spotLightShape)
                    {
                        case SpotLightShape.Cone:
                            using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                            {
                                Vector3 outterAngleInnerAngleRange = new Vector3(light.spotAngle, light.spotAngle * additionalData.innerSpotPercent01, light.range);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = wireframeColorBehind;
                                CoreLightEditorUtilities.DrawSpotlightWireframe(outterAngleInnerAngleRange, additionalData.shadowNearPlane);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = wireframeColorAbove;
                                CoreLightEditorUtilities.DrawSpotlightWireframe(outterAngleInnerAngleRange, additionalData.shadowNearPlane);
                                EditorGUI.BeginChangeCheck();
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = handleColorBehind;
                                outterAngleInnerAngleRange = CoreLightEditorUtilities.DrawSpotlightHandle(outterAngleInnerAngleRange);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = handleColorAbove;
                                outterAngleInnerAngleRange = CoreLightEditorUtilities.DrawSpotlightHandle(outterAngleInnerAngleRange);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObjects(new UnityEngine.Object[] { light, additionalData }, "Adjust Cone Spot Light");
                                    additionalData.innerSpotPercent = 100f * outterAngleInnerAngleRange.y / Mathf.Max(0.1f, outterAngleInnerAngleRange.x);
                                    light.spotAngle = outterAngleInnerAngleRange.x;
                                    light.range = outterAngleInnerAngleRange.z;
                                }

                                // Handles.color reseted at end of scope
                            }
                            break;
                        case SpotLightShape.Pyramid:
                            using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                            {
                                Vector4 aspectFovMaxRangeMinRange = new Vector4(additionalData.aspectRatio, light.spotAngle, light.range);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = wireframeColorBehind;
                                CoreLightEditorUtilities.DrawSpherePortionWireframe(aspectFovMaxRangeMinRange, additionalData.shadowNearPlane);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = wireframeColorAbove;
                                CoreLightEditorUtilities.DrawSpherePortionWireframe(aspectFovMaxRangeMinRange, additionalData.shadowNearPlane);
                                EditorGUI.BeginChangeCheck();
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = handleColorBehind;
                                aspectFovMaxRangeMinRange = CoreLightEditorUtilities.DrawSpherePortionHandle(aspectFovMaxRangeMinRange, false);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = handleColorAbove;
                                aspectFovMaxRangeMinRange = CoreLightEditorUtilities.DrawSpherePortionHandle(aspectFovMaxRangeMinRange, false);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObjects(new UnityEngine.Object[] { light, additionalData }, "Adjust Pyramid Spot Light");
                                    additionalData.aspectRatio = aspectFovMaxRangeMinRange.x;
                                    light.spotAngle = aspectFovMaxRangeMinRange.y;
                                    light.range = aspectFovMaxRangeMinRange.z;
                                }

                                // Handles.color reseted at end of scope
                            }
                            break;
                        case SpotLightShape.Box:
                            using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                            {
                                Vector4 widthHeightMaxRangeMinRange = new Vector4(additionalData.shapeWidth, additionalData.shapeHeight, light.range);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = wireframeColorBehind;
                                CoreLightEditorUtilities.DrawOrthoFrustumWireframe(widthHeightMaxRangeMinRange, additionalData.shadowNearPlane);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = wireframeColorAbove;
                                CoreLightEditorUtilities.DrawOrthoFrustumWireframe(widthHeightMaxRangeMinRange, additionalData.shadowNearPlane);
                                EditorGUI.BeginChangeCheck();
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = handleColorBehind;
                                widthHeightMaxRangeMinRange = CoreLightEditorUtilities.DrawOrthoFrustumHandle(widthHeightMaxRangeMinRange, false);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = handleColorAbove;
                                widthHeightMaxRangeMinRange = CoreLightEditorUtilities.DrawOrthoFrustumHandle(widthHeightMaxRangeMinRange, false);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObjects(new UnityEngine.Object[] { light, additionalData }, "Adjust Box Spot Light");
                                    additionalData.shapeWidth = widthHeightMaxRangeMinRange.x;
                                    additionalData.shapeHeight = widthHeightMaxRangeMinRange.y;
                                    light.range = widthHeightMaxRangeMinRange.z;
                                }

                                // Handles.color reseted at end of scope
                            }
                            break;
                    }
                    break;
                case HDLightType.Area:
                    switch (additionalData.areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                        case AreaLightShape.Tube:
                            bool withYAxis = additionalData.areaLightShape == AreaLightShape.Rectangle;
                            using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                            {
                                Vector2 widthHeight = new Vector4(additionalData.shapeWidth, withYAxis ? additionalData.shapeHeight : 0f);
                                float range = light.range;
                                EditorGUI.BeginChangeCheck();
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = wireframeColorBehind;
                                CoreLightEditorUtilities.DrawAreaLightWireframe(widthHeight);
                                range = Handles.RadiusHandle(Quaternion.identity, Vector3.zero, range); //also draw handles
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = wireframeColorAbove;
                                CoreLightEditorUtilities.DrawAreaLightWireframe(widthHeight);
                                range = Handles.RadiusHandle(Quaternion.identity, Vector3.zero, range); //also draw handles
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = handleColorBehind;
                                widthHeight = CoreLightEditorUtilities.DrawAreaLightHandle(widthHeight, withYAxis);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = handleColorAbove;
                                widthHeight = CoreLightEditorUtilities.DrawAreaLightHandle(widthHeight, withYAxis);
                                widthHeight = Vector2.Max(Vector2.one * k_MinLightSize, widthHeight);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObjects(new UnityEngine.Object[] { light, additionalData }, withYAxis ? "Adjust Area Rectangle Light" : "Adjust Area Tube Light");
                                    additionalData.shapeWidth = widthHeight.x;
                                    if (withYAxis)
                                    {
                                        additionalData.shapeHeight = widthHeight.y;
                                    }
                                    light.range = range;
                                }

                                // Handles.color reseted at end of scope
                            }
                            break;
                        case AreaLightShape.Disc:
                            //use legacy handles for this case
                            break;
                    }
                    break;
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        static void DrawGizmoForHDAdditionalLightData(HDAdditionalLightData src, GizmoType gizmoType)
        {
            if (!(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset))
                return;

            if (src.type != HDLightType.Directional)
            {
                // Trace a ray down to better locate the light location
                Ray ray = new Ray(src.gameObject.transform.position, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                    using (new Handles.DrawingScope(Color.green))
                    {
                        Handles.DrawLine(src.gameObject.transform.position, hit.point);
                        Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);
                    }

                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                    using (new Handles.DrawingScope(Color.red))
                    {
                        Handles.DrawLine(src.gameObject.transform.position, hit.point);
                        Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);
                    }
                }
            }
        }
    }
}
