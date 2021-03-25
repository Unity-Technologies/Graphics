using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class ProjectSettingsConverter : RenderPipelineConverter
{
    public override string name => "Quality and Graphics Settings";

    public override string info =>
        "This converter will look at creating Universal Render Pipeline assets and respective renderers and set their " +
        "settings based on equivalent settings from builtin renderer.";
    public override Type conversion => typeof(BuiltInToURPConversion);

    private List<ProjectSettingItem> settingsItems = new List<ProjectSettingItem>();
    private List<CameraSettings> cameraSettings = new List<CameraSettings>();

    private bool needsForward = false;
    private bool needsDeferred = false;

    public override void OnInitialize(InitializeConverterContext context)
    {

        //checking if deferred or forward
        // check graphics tiers
        GatherGraphicsTiers();

        // find all cameras and thier info
        GatherCameras(context);

        var id = 0;
        foreach (var levelName in QualitySettings.names)
        {
            var setting = QualitySettings.GetRenderPipelineAssetAt(id);
            var item = new ConverterItemDescriptor();
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
                text = "Will Generate Pipeline Asset";
            }

            item.path = text;

            context.AddAssetToConvert(item);
            id++;
        }
    }

    public override void OnRun(RunConverterContext context)
    {
        foreach (var item in context.items)
        {
            // which one am I?
            Debug.Log($"Starting upgrade of Quality Level {item.index}:{item.descriptor.name}");

            //creating pipeline asset

            //create renderers
            if (needsForward)
            {
                Debug.Log($"Generating a forward renderer");
            }

            if (needsDeferred)
            {
                Debug.Log("Generating a deferred renderer");
            }

            //looping through all settings
        }
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

    }

    internal void GatherCameras(InitializeConverterContext context)
    {
        var cameras = Object.FindObjectsOfType<Camera>();
        // final version will take some time so...
        Thread.Sleep(100);

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
                    break;
                case RenderingPath.DeferredLighting:
                    camSettings = new CameraSettings();
                    CaptureCamera(camera, ref camSettings);
                    break;
                case RenderingPath.DeferredShading:
                    camSettings = new CameraSettings();
                    CaptureCamera(camera, ref camSettings);
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
                path = camSettings.assetPath,
                initialInfo = "",
            };
            context.AddAssetToConvert(info);
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
        public UniversalRenderPipelineAsset URPAsset;
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
