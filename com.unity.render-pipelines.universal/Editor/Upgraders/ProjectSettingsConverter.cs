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

internal class ProjectSettingsConverter : RenderPipelineConverter
{
    public override string name => "Quality and Graphics Settings";

    public override string info =>
        "This converter will look at creating Universal Render Pipeline assets and respective renderers and set their " +
        "settings based on equivalent settings from builtin renderer.";
    public override Type conversion => typeof(BuiltInToURPConverterContainer);

    private List<ProjectSettingItem> settingsItems = new List<ProjectSettingItem>();
    private List<CameraSettings> cameraSettings = new List<CameraSettings>();

    private bool needsForward = false;
    private int forwardIndex = -1;
    private bool needsDeferred = false;
    private int deferredIndex = -1;

    public override void OnInitialize(InitializeConverterContext context)
    {
        // find all cameras and thier info
        GatherCameras(ref context);

        // checking if deferred or forward
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

    internal void GatherGraphicsTiers()
    {
        var targetGrp = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        for (var i = 0; i < 3; i++)
        {
            var tier = EditorGraphicsSettings.GetTierSettings(targetGrp, i == 0 ? GraphicsTier.Tier1 : i == 1 ? GraphicsTier.Tier2 : GraphicsTier.Tier3);
            switch (tier.renderingPath)
            {
                case RenderingPath.VertexLit:
                    needsForward = true;
                    break;
                case RenderingPath.Forward:
                    needsForward = true;
                    break;
                case RenderingPath.DeferredLighting:
                    needsDeferred = true;
                    break;
                case RenderingPath.DeferredShading:
                    needsDeferred = true;
                    break;
                default:
                    needsForward = true;
                    break;
            }
        }
    }

    internal void GatherCameras(ref InitializeConverterContext context)
    {
        using var searchForwardContext = SearchService.CreateContext("asset", "p:t=camera (renderingpath=forward or renderingpath=legacyvertexlit)");
        using var forwardRequest = SearchService.Request(searchForwardContext);
        {
            // we're going to do this step twice in order to get them ordered, but it should be fast
            var orderedRequest = forwardRequest.OrderBy(req =>
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
                    info = "Needs Forward Renderer",
                };

                context.AddAssetToConvert(item);
                needsForward = true;
            }
        }

        using var searchDeferredContext = SearchService.CreateContext("asset", "p:t=camera (renderingpath=deferred or renderingpath=legacydeferred(lightprepass))");
        using var deferredRequest = SearchService.Request(searchDeferredContext);
        {
            // we're going to do this step twice in order to get them ordered, but it should be fast
            var orderedRequest = deferredRequest.OrderBy(req =>
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
                    info = "Needs Deferred Renderer",
                };

                context.AddAssetToConvert(item);
                needsDeferred = true;
            }
        }


        /*
        foreach (var camera in cameras)
        {
            CameraSettings camSettings = null;

            switch (camera.renderingPath)
            {
                case RenderingPath.UsePlayerSettings:
                    Debug.Log($"Camera {camera.name} not needed, is {camera.renderingPath}");
                    break;
                case RenderingPath.VertexLit:
                    Debug.Log($"Camera {camera.name} Render path {camera.renderingPath} is not supported.");
                    break;
                case RenderingPath.Forward:
                    camSettings = new CameraSettings();
                    CaptureCamera(camera, ref camSettings);
                    needsForward = true;
                    break;
                case RenderingPath.DeferredLighting:
                    camSettings = new CameraSettings();
                    CaptureCamera(camera, ref camSettings);
                    needsDeferred = true;
                    break;
                case RenderingPath.DeferredShading:
                    camSettings = new CameraSettings();
                    CaptureCamera(camera, ref camSettings);
                    needsDeferred = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (camSettings == null)
                return;

            cameraSettings.Add(camSettings);

            ConverterItemDescriptor info = new ConverterItemDescriptor()
            {
                name = $"Camera:{camera.name} > {camSettings.renderingPath}",
                info = camSettings.assetPath,
                warningMessage = "",
            };
            context.AddAssetToConvert(info);
            // TODO remove in final
            Thread.Sleep(100);
        }
        */
    }

    internal void GatherQualityLevels(ref InitializeConverterContext context)
    {
        var id = 0;
        foreach (var levelName in QualitySettings.names)
        {
            var projectSettings = new ProjectSettingItem();
            var setting = QualitySettings.GetRenderPipelineAssetAt(id);
            var item = new ConverterItemDescriptor();
            projectSettings.levelName = levelName;
            item.name = $"Quality Level {id}:{levelName}";

            var text = "";
            if (setting != null)
            {
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
            projectSettings.index = id;
            settingsItems.Add(projectSettings);
            context.AddAssetToConvert(item);
            id++;
        }
    }

    internal void CaptureCamera(Camera camera, ref CameraSettings camSettings)
    {
        camSettings.goName = camera.name;
        camSettings.objectID = camera.GetInstanceID();
        //render path
        camSettings.renderingPath = camera.renderingPath;

        if (PrefabUtility.IsPartOfAnyPrefab(camera))
        {
            camSettings.assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(camera);

            if (PrefabUtility.IsPartOfVariantPrefab(camera))
            {
                camSettings.parent = Parent.PrefabVariant;
                var overrides = PrefabUtility.GetObjectOverrides(camera.gameObject);
                foreach (var objectOverride in overrides)
                {
                    // figure out if the rendering mode has been overridden
                }
            }
            else
            {
                camSettings.parent = Parent.Prefab;
            }
        }
        else
        {
            camSettings.parent = Parent.Scene;
            camSettings.assetPath = camera.gameObject.scene.path;
        }
        //save path to object if !use graphics settings && not same as graphics tier
        Debug.Log($"Camera {camSettings.objectID}:{camSettings.goName} " +
                  $"is using {camSettings.renderingPath}, " +
                  $"it lives in a {camSettings.parent} at {camSettings.assetPath}.");
    }

    internal class ProjectSettingItem
    {
        public int index;
        public string levelName;
        //public UniversalRenderPipelineAsset URPAsset;
    }

    internal class CameraSettings
    {
        public string goName;
        public int objectID;
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
