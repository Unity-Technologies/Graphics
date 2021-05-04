using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Player;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using ShadowQuality = UnityEngine.ShadowQuality;
using ShadowResolution = UnityEngine.ShadowResolution;

internal class ProjectSettingsConverter : RenderPipelineConverter
{
    public override string name => "Quality and Graphics Settings";

    public override string info =>
        "This converter will look at creating Universal Render Pipeline assets and respective renderers and set their " +
        "settings based on equivalent settings from builtin renderer.";
    public override Type conversion => typeof(BuiltInToURPConverterContainer);

    private List<ProjectSettingItem> settingsItems = new List<ProjectSettingItem>();
    private List<CameraSettings> cameraSettings = new List<CameraSettings>();
    private GraphicsTierSettings graphicsTierSettings;

    private bool needsForward = false;
    private int forwardIndex = -1;
    private bool needsDeferred = false;
    private int deferredIndex = -1;

    public override void OnInitialize(InitializeConverterContext context)
    {
        // find all cameras and their info
        GatherCameras(ref context);

        // check graphics tiers
        GatherGraphicsTiers();

        // check quality levels
        GatherQualityLevels(ref context);
    }

    public override void OnRun(ref RunItemContext context)
    {
        var currentQualityLevel = QualitySettings.GetQualityLevel();

        var item = context.item;
        // is camera
        if (item.index > cameraSettings.Count - 1)
        {
            var projectSetting = settingsItems[item.index - cameraSettings.Count];
            // which one am I?
            Debug.Log($"Starting upgrade of Quality Level {projectSetting.index}:{projectSetting.levelName}");

            //creating pipeline asset
            Thread.Sleep(100);
            var asset = ScriptableObject.CreateInstance(typeof(UniversalRenderPipelineAsset)) as UniversalRenderPipelineAsset;
            if(!AssetDatabase.IsValidFolder("Assets/TestAssets"))
                AssetDatabase.CreateFolder("Assets", "TestAssets");
            var path = $"Assets/TestAssets/{projectSetting.levelName}_PipelineAsset.asset";
            AssetDatabase.CreateAsset(asset, path);

            //create renderers
            if (needsForward)
            {
                Debug.Log($"Generating a forward renderer");
                var rendererAsset = UniversalRenderPipelineAsset.CreateRendererAsset(path, RendererType.UniversalRenderer);
                //Missing API to set deferred or forward
                //missing API to assign to pipeline assetß
                forwardIndex = asset.m_RendererDataList.Length == 1 ? -1 : 1;
                // TODO remove in final
                Thread.Sleep(100);
            }

            if (needsDeferred)
            {
                Debug.Log("Generating a deferred renderer");
                var rendererAsset = UniversalRenderPipelineAsset.CreateRendererAsset(path, RendererType.UniversalRenderer);
                //Missing API to set deferred or forward
                //missing API to assign to pipeline asset
                deferredIndex = asset.m_RendererDataList.Length == 1 ? -1 : asset.m_RendererDataList.Length - 1;
                // TODO remove in final
                Thread.Sleep(100);
            }

            //looping through all settings

            //assign asset
            QualitySettings.SetQualityLevel(projectSetting.index);
            QualitySettings.renderPipeline = asset;
        }
        else // is quality level
        {
            if (cameraSettings[item.index].renderingPath == RenderingPath.Forward)
            {
                needsForward = true;
            }
            if (cameraSettings[item.index].renderingPath == RenderingPath.DeferredLighting ||
                cameraSettings[item.index].renderingPath == RenderingPath.DeferredShading)
            {
                needsDeferred = true;
            }
        }

        QualitySettings.SetQualityLevel(currentQualityLevel);
    }

    public override void OnClicked(int index)
    {
        if (index > cameraSettings.Count - 1)
        {
            // in quality levels
            SettingsService.OpenProjectSettings("Project/Quality");
        }
        else
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>(cameraSettings[index].assetPath);

            if(obj)
                EditorGUIUtility.PingObject(obj);
        }
    }

    /// <summary>
    /// Grabs the 3rd tier from the Graphics Tier Settings based off the current build platform
    /// </summary>
    private void GatherGraphicsTiers()
    {
        var targetGrp = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        var tier = EditorGraphicsSettings.GetTierSettings(targetGrp, GraphicsTier.Tier1);

        graphicsTierSettings.RenderingPath = tier.renderingPath;
        graphicsTierSettings.ReflectionProbeBlending = tier.reflectionProbeBlending;
        graphicsTierSettings.ReflectionProbeBoxProjection = tier.reflectionProbeBoxProjection;
        graphicsTierSettings.CascadeShadows = tier.cascadedShadowMaps;
        graphicsTierSettings.HDR = tier.hdr;
    }

    /// <summary>
    /// Searches and finds any cameras that have dedicated Render Path values, i.e not using hte Graphics Settings option.
    /// </summary>
    /// <param name="context">Converter context to fill in.</param>
    private void GatherCameras(ref InitializeConverterContext context)
    {
        using var searchForwardContext = SearchService.CreateContext("asset", "p:t=camera (renderingpath=forward or renderingpath=legacyvertexlit)");
        using var forwardRequest = SearchService.Request(searchForwardContext);
        {
            AddCamera(forwardRequest, ref context, RenderingPath.Forward);
        }

        using var searchDeferredContext = SearchService.CreateContext("asset", "p:t=camera (renderingpath=deferred or renderingpath=legacydeferred(lightprepass))");
        using var deferredRequest = SearchService.Request(searchDeferredContext);
        {
            AddCamera(deferredRequest, ref context, RenderingPath.DeferredShading);
        }
    }

    private void AddCamera(ISearchList searchList, ref InitializeConverterContext context, RenderingPath path)
    {
        // we're going to do this step twice in order to get them ordered, but it should be fast
        var orderedRequest = searchList.OrderBy(req =>
            {
                GlobalObjectId.TryParse(req.id, out var gid);
                return gid.assetGUID;
            })
            .ToList();

        foreach (var r in orderedRequest)
        {
            if (r == null || !GlobalObjectId.TryParse(r.id, out var gid))
            {
                continue;
            }

            var label = r.provider.fetchLabel(r, r.context);
            var description = r.provider.fetchDescription(r, r.context);

            var item = new ConverterItemDescriptor()
            {
                name = $"{label} : {description}",
                info = $"Needs {path}",
            };

            context.AddAssetToConvert(item);

            var camItem = new CameraSettings()
            {
                goName = label,
                objectID = gid,
                assetPath = "unknown",
                parent = Parent.Scene,
                renderingPath = path,
            };
            cameraSettings.Add(camItem);
        }
    }

    private void GatherQualityLevels(ref InitializeConverterContext context)
    {
        var id = 0;
        foreach (var levelName in QualitySettings.names)
        {
            var projectSettings = new ProjectSettingItem
            {
                index = id,
                levelName = levelName,
                pixelLightCount = QualitySettings.pixelLightCount,
                MSAA = QualitySettings.antiAliasing,
                softShadows = QualitySettings.shadows == ShadowQuality.All ? true : false,
                shadowResolution = QualitySettings.shadowResolution,
                shadowDistance = QualitySettings.shadowDistance,
                shadowCascadeCount = QualitySettings.shadowCascades,
                cascadeSplit1 = QualitySettings.shadowCascades == 1 ? QualitySettings.shadowCascade2Split : QualitySettings.shadowCascade4Split.x,
                cascadeSplit2 = QualitySettings.shadowCascade4Split.y,
                cascadeSplit3 = QualitySettings.shadowCascade4Split.z,
            };
            settingsItems.Add(projectSettings);

            var setting = QualitySettings.GetRenderPipelineAssetAt(id);
            var item = new ConverterItemDescriptor();
            item.name = $"Quality Level {id}:{levelName}";

            var text = "";
            if (setting != null)
            {
                item.warningMessage = "Contains SRP Asset already, can override if desired.";

                if (setting.GetType().ToString().Contains("Universal.UniversalRenderPipelineAsset"))
                {
                    text = "Contains URP Asset, will override existing asset.";
                }
                else
                {
                    text = "Contains SRP Asset, will override existing asset with URP asset.";
                }
            }
            else
            {
                text = "Will Generate Pipeline Asset, ";
                if (needsForward && needsDeferred)
                {
                    text += "Forward & Deferred Renderer Assets.";
                }
                else if (needsForward)
                {
                    text += "Forward Renderer Asset.";
                }
                else if (needsDeferred)
                {
                    text += "Deferred Renderer Asset.";
                }
            }
            item.info = text;
            context.AddAssetToConvert(item);
            id++;
        }
    }

    private struct GraphicsTierSettings
    {
        public bool ReflectionProbeBoxProjection;
        public bool ReflectionProbeBlending;
        public bool CascadeShadows;
        public bool HDR;
        public RenderingPath RenderingPath;
    }

    private class ProjectSettingItem
    {
        // General
        public int index;
        public string levelName;
        // Settings
        public int pixelLightCount;
        public int MSAA;
        public bool softShadows;
        public ShadowResolution shadowResolution;
        public float shadowDistance;
        public int shadowCascadeCount;
        public float cascadeSplit1;
        public float cascadeSplit2;
        public float cascadeSplit3;
    }

    internal class CameraSettings
    {
        public string goName;
        public GlobalObjectId objectID;
        public Parent parent;
        public string assetPath;
        public RenderingPath renderingPath;
    }

    internal enum Parent
    {
        Prefab,
        PrefabVariant,
        Scene,
    }
}
