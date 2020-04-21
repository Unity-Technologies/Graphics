using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(IESImporter))]
    public class HDIESImporterEditor : UnityEditor.Rendering.IESImporterEditor
    {
        public override void LayoutRenderPipelineUseIesMaximumIntensity()
        {
            // Before enabling this feature, more experimentation is needed with the addition of a Volume in the PreviewRenderUtility scene.

            // EditorGUILayout.PropertyField(m_UseIesMaximumIntensityProp, new GUIContent("Use IES Maximum Intensity"));
        }

        public override void SetupRenderPipelinePreviewCamera(Camera camera)
        {
            HDAdditionalCameraData hdCamera = camera.gameObject.AddComponent<HDAdditionalCameraData>();

            hdCamera.clearDepth     = true;
            hdCamera.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;

            hdCamera.GetType().GetProperty("isEditorCameraPreview", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(hdCamera, true, null);
        }

        public override void SetupRenderPipelinePreviewLight(Light light)
        {
            HDLightTypeAndShape hdLightTypeAndShape = (light.type == LightType.Point) ? HDLightTypeAndShape.Point : HDLightTypeAndShape.ConeSpot;

            HDAdditionalLightData hdLight = GameObjectExtension.AddHDLight(light.gameObject, hdLightTypeAndShape);

            hdLight.SetIntensity(20000f, LightUnit.Lumen);

            hdLight.affectDiffuse     = true;
            hdLight.affectSpecular    = false;
            hdLight.affectsVolumetric = false;
        }

        public override void SetupRenderPipelinePreviewWallRenderer(MeshRenderer wallRenderer)
        {
            wallRenderer.material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat");
        }

        public override void SetupRenderPipelinePreviewFloorRenderer(MeshRenderer floorRenderer)
        {
            floorRenderer.material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat");
        }

        public override void SetupRenderPipelinePreviewLightIntensity(Light light)
        {
            // Before enabling this feature, more experimentation is needed with the addition of a Volume in the PreviewRenderUtility scene.

            // HDAdditionalLightData hdLight = light.GetComponent<HDAdditionalLightData>();
            //
            // if (m_UseIesMaximumIntensityProp.boolValue)
            // {
            //     LightUnit lightUnit = (m_IesMaximumIntensityUnitProp.stringValue == "Lumens") ? LightUnit.Lumen : LightUnit.Candela;
            //     hdLight.SetIntensity(m_IesMaximumIntensityProp.floatValue, lightUnit);
            // }
            // else
            // {
            //     hdLight.SetIntensity(20000f, LightUnit.Lumen);
            // }
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("IES Luminaire Profile");
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Texture cookieTexture  = null;
                Texture previewTexture = null;

                foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath((target as IESImporter).assetPath))
                {
                    if (subAsset.name.EndsWith("-Cookie"))
                    {
                        cookieTexture = subAsset as Texture;
                        break;
                    }
                }

                if (cookieTexture != null)
                {
                    m_PreviewRenderUtility.lights[0].transform.localEulerAngles = new Vector3(90f, 0f, m_LightAimAxisRotationProp.floatValue);
                    SetupRenderPipelinePreviewLightIntensity(m_PreviewRenderUtility.lights[0]);
                    m_PreviewRenderUtility.lights[0].cookie = cookieTexture;

                    m_PreviewRenderUtility.BeginPreview(r, background);

                    bool fog = RenderSettings.fog;
                    Unsupported.SetRenderSettingsUseFogNoDirty(false);

                    m_PreviewRenderUtility.camera.Render();

                    Unsupported.SetRenderSettingsUseFogNoDirty(fog);

                    previewTexture = m_PreviewRenderUtility.EndPreview();
                }

                if (previewTexture == null)
                {
                    GUI.DrawTexture(r, Texture2D.blackTexture, ScaleMode.StretchToFill, false);
                }
                else
                {
                    GUI.DrawTexture(r, previewTexture, ScaleMode.ScaleToFit, false);
                }
            }
        }
    }
}
