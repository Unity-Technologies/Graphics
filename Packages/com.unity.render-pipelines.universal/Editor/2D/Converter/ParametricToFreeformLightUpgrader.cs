using System;
using System.Collections.Generic;
using UnityEditor.Rendering.Converter;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Categorization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    internal class ParametricToFreeformLightUpgraderItem : RenderPipelineConverterAssetItem
    {
        public int type { get; set; }

        public ParametricToFreeformLightUpgraderItem(string id) : base(id)
        {
        }

        public ParametricToFreeformLightUpgraderItem(GlobalObjectId gid, string assetPath) : base(gid, assetPath)
        {
        }

        public new Texture2D icon => EditorGUIUtility.ObjectContent(null, typeof(UnityEngine.Rendering.Universal.Light2D)).image as Texture2D;
    }

    [Serializable]
    [PipelineTools]
    [BatchModeConverterClassInfo("UpgradeURP2DAssets", "ParametricToFreeformLight")]
    [ElementInfo(Name = "Parametric to Freeform Light Upgrade",
             Order = 100,
             Description = "This will upgrade all parametric lights to freeform lights.")]
    internal sealed class ParametricToFreeformLightUpgrader : IRenderPipelineConverter
    {
        public bool isEnabled
        {
            get
            {
                var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (urpAsset == null)
                    return false;

                var renderer = urpAsset.scriptableRenderer as Renderer2D;
                if (renderer == null)
                    return false;

                return true;
            }
        }
        public string isDisabledMessage => "The current Render Pipeline is not URP or the current Renderer is not 2D";

        public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
        {
            var returnList = new List<IRenderPipelineConverterItem>();
            void OnSearchFinish()
            {
                onScanFinish?.Invoke(returnList);
            }

            var processedIds = new HashSet<string>();

            SearchServiceUtils.RunQueuedSearch
            (
                SearchServiceUtils.IndexingOptions.DeepSearch,
                new List<(string, string)>()
                {
                    ("t:UnityEngine.Rendering.Universal.Light2D", "Light 2D"),
                },
                (item, description) =>
                {
                    // Direct conversion - works for both assets and scene objects
                    var unityObject = item.ToObject();

                    if (unityObject == null)
                        return;

                    // Ensure we're always working with GameObjects
                    GameObject go = null;

                    if (unityObject is GameObject gameObject)
                        go = gameObject;
                    else if (unityObject is Component component)
                        go = component.gameObject;
                    else
                        return; // Not a GameObject or Component

                    var gid = GlobalObjectId.GetGlobalObjectIdSlow(go);
                    if (!processedIds.Add(gid.ToString()))
                        return;

                    int type = gid.identifierType; // 1=Asset, 2=SceneObject

                    var isPrefab = type == 1;
                    var lightUpgraderItem = new ParametricToFreeformLightUpgraderItem(gid.ToString())
                    {
                        info = isPrefab
                            ? $"Prefab: {AssetDatabase.GetAssetPath(unityObject)}"
                            : $"Scene: {go.scene.path}",
                        type = type
                    };


                    returnList.Add(lightUpgraderItem);
                },
                OnSearchFinish
            );
        }

        const float k_EnscribedSquareDiagonalLength = 0.70710678118654752440084436210485f;

        public static void UpgradeParametricLight(Light2D light)
        {
            if (light.lightType == (Light2D.LightType)Light2D.DeprecatedLightType.Parametric)
            {
                light.lightType = Light2D.LightType.Freeform;

                // Parametric radius has to be > 0 in order mesh tessellation to be valid
                if (light.shapeLightParametricRadius == 0)
                    light.shapeLightParametricRadius = 0.01f;

                float radius = light.shapeLightParametricRadius;
                float angle = light.shapeLightParametricAngleOffset;
                int sides = light.shapeLightParametricSides;

                var angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * angle;
                if (sides < 3)
                {
                    radius = k_EnscribedSquareDiagonalLength * radius;
                    sides = 4;
                }

                if (sides == 4)
                {
                    angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * angle;
                }

                var radiansPerSide = 2 * Mathf.PI / sides;
                var min = new Vector3(float.MaxValue, float.MaxValue, 0);
                var max = new Vector3(float.MinValue, float.MinValue, 0);


                Vector3[] shapePath = new Vector3[sides];
                for (var i = 0; i < sides; i++)
                {
                    var endAngle = (i + 1) * radiansPerSide;
                    var extrudeDir = new Vector3(Mathf.Cos(endAngle + angleOffset), Mathf.Sin(endAngle + angleOffset), 0);
                    var endPoint = radius * extrudeDir;

                    shapePath[i] = endPoint;
                }

                light.shapePath = shapePath;
                light.UpdateMesh();
                EditorSceneManager.MarkSceneDirty(light.gameObject.scene);
            }
        }

        void UpgradeGameObject(GameObject go)
        {
            Light2D[] lights = go.GetComponentsInChildren<Light2D>();
            foreach (Light2D light in lights)
            {
                if (light.lightType == Light2D.LightType.Parametric && !PrefabUtility.IsPartOfPrefabInstance(light))
                    UpgradeParametricLight(light);
            }
        }

        public Status Convert(IRenderPipelineConverterItem item, out string message)
        {
            if (item is ParametricToFreeformLightUpgraderItem lightItem)
            {
                if (lightItem.type == 1) URP2DConverterUtility.UpgradePrefab(lightItem.assetPath, UpgradeGameObject);
                else URP2DConverterUtility.UpgradeScene(lightItem.assetPath, UpgradeGameObject);
            }

            message = string.Empty;
            return Status.Success;
        }
    }
}
