using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDIESImporter))]
    public partial class HDIESImporterEditor : ScriptedImporterEditor
    {
        public UnityEditor.Rendering.IESImporterEditor iesImporterEditor = new UnityEditor.Rendering.IESImporterEditor();

        internal void SetupRenderPipelinePreviewCamera(Camera camera)
        {
            HDAdditionalCameraData hdCamera = camera.gameObject.AddComponent<HDAdditionalCameraData>();

            hdCamera.clearDepth     = true;
            hdCamera.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;

            hdCamera.GetType().GetProperty("isEditorCameraPreview", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(hdCamera, true, null);
        }

        internal void SetupRenderPipelinePreviewLight(Light light)
        {
            HDLightTypeAndShape hdLightTypeAndShape = (light.type == LightType.Point) ? HDLightTypeAndShape.Point : HDLightTypeAndShape.ConeSpot;

            HDAdditionalLightData hdLight = GameObjectExtension.AddHDLight(light.gameObject, hdLightTypeAndShape);

            hdLight.SetIntensity(20000f, LightUnit.Lumen);

            hdLight.affectDiffuse     = true;
            hdLight.affectSpecular    = false;
            hdLight.affectsVolumetric = false;
        }

        internal void SetupRenderPipelinePreviewWallRenderer(MeshRenderer wallRenderer)
        {
            wallRenderer.material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat");
        }

        internal void SetupRenderPipelinePreviewFloorRenderer(MeshRenderer floorRenderer)
        {
            floorRenderer.material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat");
        }

        internal void SetupRenderPipelinePreviewLightIntensity(Light light, SerializedProperty useIESMaximumIntensityProp, SerializedProperty iesMaximumIntensityUnitProp, SerializedProperty iesMaximumIntensityProp)
        {
            // Before enabling this feature, more experimentation is needed with the addition of a Volume in the PreviewRenderUtility scene.

            HDAdditionalLightData hdLight = light.GetComponent<HDAdditionalLightData>();

            if (useIESMaximumIntensityProp.boolValue)
            {
                LightUnit lightUnit = (iesMaximumIntensityUnitProp.stringValue == "Lumens") ? LightUnit.Lumen : LightUnit.Candela;
                hdLight.SetIntensity(iesMaximumIntensityProp.floatValue, lightUnit);
            }
            else
            {
                hdLight.SetIntensity(20000f, LightUnit.Lumen);
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var entryPoint0 = new PropertyFetcher<HDIESImporter>(serializedObject);
            SerializedProperty entryPoint1 = entryPoint0.Find<IESImporter>(x => x.commonIESImporter);
            Debug.Log("001");
            SerializedProperty entryPoint = entryPoint1.FindPropertyRelative("iesMetaData");

            iesImporterEditor.CommonOnEnable(entryPoint);
        }

        public override void OnInspectorGUI()
        {
            iesImporterEditor.CommonOnInspectorGUI(this as ScriptedImporterEditor);

            base.ApplyRevertGUI();
        }

        protected override void Apply()
        {
            base.Apply();

            iesImporterEditor.CommonApply();
        }

        public override bool HasPreviewGUI()
        {
            return iesImporterEditor.CommonHasPreviewGUI(
                    delegate (Camera camera)
                    {
                        SetupRenderPipelinePreviewCamera(camera);
                    },
                    delegate (Light light)
                    {
                        SetupRenderPipelinePreviewLight(light);
                    },
                    delegate (MeshRenderer wallRenderer)
                    {
                        SetupRenderPipelinePreviewWallRenderer(wallRenderer);
                    },
                    delegate (MeshRenderer floorRenderer)
                    {
                        SetupRenderPipelinePreviewFloorRenderer(floorRenderer);
                    }
                );
        }

        public override GUIContent GetPreviewTitle()
        {
            return iesImporterEditor.CommonGetPreviewTitle();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            iesImporterEditor.CommonOnPreviewGUI(r, background, target as HDIESImporter,
                                    delegate (Light light, SerializedProperty useIESMaximumIntensityProp, SerializedProperty iesMaximumIntensityUnitProp, SerializedProperty iesMaximumIntensityProp)
                                    {
                                        SetupRenderPipelinePreviewLightIntensity(light, useIESMaximumIntensityProp, iesMaximumIntensityUnitProp, iesMaximumIntensityProp);
                                    });
        }

        public override void OnDisable()
        {
            base.OnDisable();

            iesImporterEditor.CommonOnDisable();
        }
    }
}
