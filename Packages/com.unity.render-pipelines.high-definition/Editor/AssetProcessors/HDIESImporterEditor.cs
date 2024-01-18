using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Class describing the logic for importer an IES file an generating the IESObject associated
    /// </summary>
    [CustomEditor(typeof(IESImporter))]
    public partial class HDIESImporterEditor : ScriptedImporterEditor
    {
        /// <summary>
        /// IES Importer Editor, common to Core and HDRP
        /// </summary>
        public UnityEditor.Rendering.IESImporterEditor iesImporterEditor = new UnityEditor.Rendering.IESImporterEditor();

        internal void SetupRenderPipelinePreviewCamera(Camera camera)
        {
            HDAdditionalCameraData hdCamera = camera.gameObject.AddComponent<HDAdditionalCameraData>();

            hdCamera.clearDepth = true;
            hdCamera.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;

            hdCamera.GetType().GetProperty("isEditorCameraPreview", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(hdCamera, true, null);
        }

        internal void SetupRenderPipelinePreviewLight(Light light)
        {
            LightType lightType = (light.type == LightType.Point) ? LightType.Point : LightType.Spot;
            HDAdditionalLightData hdLight = GameObjectExtension.AddHDLight(light.gameObject, lightType);

            light.intensity = LightUnitUtils.ConvertIntensity(light, 20000f, LightUnit.Lumen, LightUnit.Candela);

            hdLight.affectDiffuse = true;
            hdLight.affectSpecular = false;
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
            if (useIESMaximumIntensityProp.boolValue)
            {
                LightUnit lightUnit = (iesMaximumIntensityUnitProp.stringValue == "Lumens") ? LightUnit.Lumen : LightUnit.Candela;
                light.intensity = LightUnitUtils.ConvertIntensity(light, iesMaximumIntensityProp.floatValue, lightUnit, LightUnit.Candela);
            }
            else
            {
                light.intensity = LightUnitUtils.ConvertIntensity(light, 20000f, LightUnit.Lumen, LightUnit.Candela);
            }
        }

        /// <summary>
        /// Call back for ScriptedImporterEditor
        /// </summary>
        public override void OnEnable()
        {
            base.OnEnable();

            PropertyFetcher<IESImporter> entryPoint0 = new PropertyFetcher<IESImporter>(serializedObject);
            SerializedProperty entryPoint1 = entryPoint0.Find<IESMetaData>(x => x.iesMetaData);
            //SerializedProperty entryPoint = entryPoint1.FindPropertyRelative("iesMetaData");

            iesImporterEditor.CommonOnEnable(entryPoint1);
        }

        /// <summary>
        /// Call back for ScriptedImporterEditor
        /// </summary>
        public override void OnInspectorGUI()
        {
            iesImporterEditor.CommonOnInspectorGUI(this as ScriptedImporterEditor);

            base.ApplyRevertGUI();
        }

        /// <summary>
        /// Call back for ScriptedImporterEditor
        /// </summary>
        protected override void Apply()
        {
            base.Apply();

            iesImporterEditor.CommonApply();
        }

        /// <summary>
        /// Call back for ScriptedImporterEditor
        /// </summary>
        /// <returns>If this importer has a preview or not</returns>
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

        /// <summary>
        /// Call back for ScriptedImporterEditor
        /// </summary>
        /// <returns>The title of the Preview</returns>
        public override GUIContent GetPreviewTitle()
        {
            return iesImporterEditor.CommonGetPreviewTitle();
        }

        /// <summary>
        /// Call back for ScriptedImporterEditor
        /// </summary>
        /// <param name="r">Rectangle of the preview.</param>
        /// <param name="background">Style of the background of the preview.</param>
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            iesImporterEditor.CommonOnPreviewGUI(r, background, target as IESImporter,
                delegate (Light light, SerializedProperty useIESMaximumIntensityProp, SerializedProperty iesMaximumIntensityUnitProp, SerializedProperty iesMaximumIntensityProp)
                {
                    SetupRenderPipelinePreviewLightIntensity(light, useIESMaximumIntensityProp, iesMaximumIntensityUnitProp, iesMaximumIntensityProp);
                });
        }

        /// <summary>
        /// Call back for ScriptedImporterEditor
        /// </summary>
        public override void OnDisable()
        {
            base.OnDisable();

            iesImporterEditor.CommonOnDisable();
        }
    }
}
