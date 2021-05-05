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

    private GraphicsTierSettings graphicsTierSettings;
    private List<SettingsItem> settingsItems = new List<SettingsItem>();

    private RenderingPath defaultRenderingPath = RenderingPath.Forward;
    private bool needsForward = false;
    private int forwardIndex = -1;
    private bool needsDeferred = false;
    private int deferredIndex = -1;

    private const string pipelineAssetPath = "TestAssets";

    public override void OnInitialize(InitializeConverterContext context)
    {
        // check graphics tiers
        GatherGraphicsTiers();

        // find all cameras and their info
        GatherCameras(ref context);

        // check quality levels
        GatherQualityLevels(ref context);
    }

    /// <summary>
    /// Grabs the 3rd tier from the Graphics Tier Settings based off the current build platform
    /// </summary>
    private void GatherGraphicsTiers()
    {
        var targetGrp = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        var tier = EditorGraphicsSettings.GetTierSettings(targetGrp, GraphicsTier.Tier1);

        switch (tier.renderingPath)
        {
            case RenderingPath.VertexLit:
            case RenderingPath.Forward:
                defaultRenderingPath = RenderingPath.Forward;
                needsForward = true;
                break;
            case RenderingPath.DeferredLighting:
            case RenderingPath.DeferredShading:
                defaultRenderingPath = RenderingPath.DeferredShading;
                needsDeferred = true;
                break;
        }

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

            var camItem = new CameraSettingItem()
            {
                goName = label,
                objectID = gid,
                assetPath = "unknown",
                parent = Parent.Scene,
                renderingPath = path,
            };
            settingsItems.Add(camItem);
        }
    }

    private void GatherQualityLevels(ref InitializeConverterContext context)
    {
        var currentQuality = QualitySettings.GetQualityLevel();
        var id = 0;
        foreach (var levelName in QualitySettings.names)
        {
            QualitySettings.SetQualityLevel(id);

            var projectSettings = new ProjectSettingItem
            {
                index = id,
                levelName = levelName,
                pixelLightCount = QualitySettings.pixelLightCount,
                MSAA = QualitySettings.antiAliasing,
                shadows = QualitySettings.shadows,
                shadowResolution = QualitySettings.shadowResolution,
                shadowDistance = QualitySettings.shadowDistance,
                shadowCascadeCount = QualitySettings.shadowCascades,
                cascadeSplit2 = QualitySettings.shadowCascade2Split,
                cascadeSplit4 = QualitySettings.shadowCascade4Split,
                softParticles = QualitySettings.softParticles,
            };
            settingsItems.Add(projectSettings);

            var setting = QualitySettings.GetRenderPipelineAssetAt(id);
            var item = new ConverterItemDescriptor();
            item.name = $"Quality Level {id}:{levelName}";

            var text = "";
            if (setting != null)
            {
                item.warningMessage = "Contains SRP Asset already.";

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
        QualitySettings.SetQualityLevel(currentQuality);
    }

    public override void OnRun(ref RunItemContext context)
    {
        var item = context.item;
        // is quality item
        if (settingsItems[item.index].GetType() == typeof(ProjectSettingItem))
        {
            GeneratePipelineAsset(item.index);
        }
        else if (settingsItems[item.index].GetType() == typeof(CameraSettingItem))// is camera
        {
            /*
            var cameraSetting = settingsItems[item.index] as CameraSettingItem;
            if (cameraSetting.renderingPath == RenderingPath.Forward)
            {
                needsForward = true;
            }
            if (cameraSetting.renderingPath == RenderingPath.DeferredLighting ||
                cameraSetting.renderingPath == RenderingPath.DeferredShading)
            {
                needsDeferred = true;
            }
            */
        }
    }

    private void GeneratePipelineAsset(int index)
    {
        // store current quality level
        var currentQualityLevel = QualitySettings.GetQualityLevel();

        // get the project setting we are working with
        var projectSetting = settingsItems[index] as ProjectSettingItem;
        Debug.Log($"Starting upgrade of Quality Level {projectSetting.index}:{projectSetting.levelName}");

        //creating pipeline asset
        var asset = ScriptableObject.CreateInstance(typeof(UniversalRenderPipelineAsset)) as UniversalRenderPipelineAsset;
        if(!AssetDatabase.IsValidFolder($"Assets/{pipelineAssetPath}"))
            AssetDatabase.CreateFolder("Assets", pipelineAssetPath);
        var path = $"Assets/{pipelineAssetPath}/{projectSetting.levelName}_PipelineAsset.asset";

        // Setting Pipeline Asset settings
        SetPipelineSettings(asset, projectSetting);

        //create renderers
        var renderers = new List<ScriptableRendererData>();
        if (needsForward)
        {
            renderers.Add(CreateRendererDataAsset(path, RenderingPath.Forward, "ForwardRenderer"));
        }

        if (needsDeferred)
        {
            renderers.Add(CreateRendererDataAsset(path, RenderingPath.DeferredShading, "DeferredRenderer"));
        }

        asset.m_RendererDataList = renderers.ToArray();

        // create asset on disk
        AssetDatabase.CreateAsset(asset, path);
        //assign asset
        QualitySettings.SetQualityLevel(projectSetting.index);
        QualitySettings.renderPipeline = asset;

        // return to original quality level
        QualitySettings.SetQualityLevel(currentQualityLevel);
    }

    private ScriptableRendererData CreateRendererDataAsset(string assetPath, RenderingPath renderingPath, string name)
    {
        Debug.Log("Generating a deferred renderer");
        var rendererAsset = UniversalRenderPipelineAsset.CreateRendererAsset(assetPath, RendererType.UniversalRenderer, true, name) as UniversalRendererData;
        //Missing API to set deferred or forward
        rendererAsset.renderingMode = renderingPath == RenderingPath.Forward ? RenderingMode.Forward : RenderingMode.Deferred;
        //missing API to assign to pipeline asset
        return rendererAsset;
    }

    /// <summary>
    /// Sets all relevant RP settings in order they appear in URP
    /// </summary>
    /// <param name="asset">Pipeline asset to set</param>
    /// <param name="settings">The ProjectSettingItem with stored settings</param>
    private void SetPipelineSettings(UniversalRenderPipelineAsset asset, ProjectSettingItem settings)
    {
        // General
        asset.supportsCameraDepthTexture = settings.softParticles;

        // Quality
        asset.supportsHDR = graphicsTierSettings.HDR;
        asset.msaaSampleCount = settings.MSAA == 0 ? 1 : settings.MSAA;

        // Main Light
        asset.mainLightRenderingMode = settings.pixelLightCount == 0
            ? LightRenderingMode.Disabled
            : LightRenderingMode.PerPixel;
        asset.supportsMainLightShadows = settings.shadows != ShadowQuality.Disable;
        asset.mainLightShadowmapResolution = GetEquivalentShadowResolution((int)settings.shadowResolution);

        // Additional Lights
        asset.additionalLightsRenderingMode = settings.pixelLightCount <= 1
            ? LightRenderingMode.Disabled
            : LightRenderingMode.PerPixel;
        asset.maxAdditionalLightsCount = Mathf.Max(0, settings.pixelLightCount - 1);
        asset.supportsAdditionalLightShadows = settings.shadows != ShadowQuality.Disable;
        asset.additionalLightsShadowmapResolution = GetEquivalentShadowResolution((int)settings.shadowResolution);

        // Reflection Probes
        asset.reflectionProbeBlending = graphicsTierSettings.ReflectionProbeBlending;
        asset.reflectionProbeBoxProjection = graphicsTierSettings.ReflectionProbeBoxProjection;

        // Shadows
        asset.shadowDistance = settings.shadowDistance;
        asset.shadowCascadeCount = settings.shadowCascadeCount;
        asset.cascade2Split = settings.cascadeSplit2;
        asset.cascade4Split = settings.cascadeSplit4;
        asset.supportsSoftShadows = settings.shadows == ShadowQuality.All;
    }

    private static int GetEquivalentShadowResolution(int value)
    {
        return value switch
        {
            0 => // low
                1024,
            1 => // med
                2048,
            2 => // high
                4096,
            3 => // very high
                4096,
            _ => 1024
        };
    }

    #region Structs

    private struct GraphicsTierSettings
    {
        public bool ReflectionProbeBoxProjection;
        public bool ReflectionProbeBlending;
        public bool CascadeShadows;
        public bool HDR;
        public RenderingPath RenderingPath;
    }

    private class SettingsItem { }

    private class ProjectSettingItem : SettingsItem
    {
        // General
        public int index;
        public string levelName;
        // Settings
        public int pixelLightCount;
        public int MSAA;
        public ShadowQuality shadows;
        public ShadowResolution shadowResolution;
        public float shadowDistance;
        public int shadowCascadeCount;
        public float cascadeSplit2;
        public Vector3 cascadeSplit4;
        public bool softParticles;
    }

    private class CameraSettingItem : SettingsItem
    {
        public string goName;
        public GlobalObjectId objectID;
        public Parent parent;
        public string assetPath;
        public RenderingPath renderingPath;
    }

    #endregion

    private enum Parent
    {
        Prefab,
        PrefabVariant,
        Scene,
    }
}
