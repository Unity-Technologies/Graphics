using System;
using System.Collections.Generic;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Categorization;

using URPPackage = UnityEngine.Rendering.Universal;

#if PIXEL_PERFECT_2D_EXISTS
using U2DPackage = UnityEngine.U2D;
#endif

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    internal class PixelPerfectCameraConverterItem : RenderPipelineConverterAssetItem
    {
        public PixelPerfectCameraConverterItem(string id) : base(id)
        {
        }

        public PixelPerfectCameraConverterItem(GlobalObjectId gid, string assetPath) : base(gid, assetPath)
        {
        }

        public new Texture2D icon => EditorGUIUtility.ObjectContent(null, typeof(UnityEngine.Camera)).image as Texture2D;
    }

    [Serializable]
    [PipelineConverter("Built-in", "Universal Render Pipeline (2D Renderer)")]
    [ElementInfo(Name = "Pixel Perfect Camera Converter",
             Order = 1000,
             Description = "This will upgrade all 2D Pixel Perfect Camera (com.unity.2d.pixelperfect) to the Universal Render Pipeline version.")]
    internal class BuiltInPixelPerfectCameraConverter : IRenderPipelineConverter
    {

#if PIXEL_PERFECT_2D_EXISTS
        public bool isEnabled => true;
        public string isDisabledMessage { get; }

        public static bool UpgradePixelPerfectCamera(U2DPackage.PixelPerfectCamera cam)
        {
            if (cam == null)
                return false;

            // Copy over serialized data
            var urpCam = cam.gameObject.AddComponent<URPPackage.PixelPerfectCamera>();

            if (urpCam == null)
                return false;

            urpCam.assetsPPU = cam.assetsPPU;
            urpCam.refResolutionX = cam.refResolutionX;
            urpCam.refResolutionY = cam.refResolutionY;

            if (cam.upscaleRT)
                urpCam.gridSnapping = URPPackage.PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            else if (cam.pixelSnapping)
                urpCam.gridSnapping = URPPackage.PixelPerfectCamera.GridSnapping.PixelSnapping;

            if (cam.cropFrameX && cam.cropFrameY)
            {
                if (cam.stretchFill)
                    urpCam.cropFrame = URPPackage.PixelPerfectCamera.CropFrame.StretchFill;
                else
                    urpCam.cropFrame = URPPackage.PixelPerfectCamera.CropFrame.Windowbox;
            }
            else if (cam.cropFrameX)
            {
                urpCam.cropFrame = URPPackage.PixelPerfectCamera.CropFrame.Pillarbox;
            }
            else if (cam.cropFrameY)
            {
                urpCam.cropFrame = URPPackage.PixelPerfectCamera.CropFrame.Letterbox;
            }
            else
            {
                urpCam.cropFrame = URPPackage.PixelPerfectCamera.CropFrame.None;
            }

            UnityEngine.Object.DestroyImmediate(cam, true);

            EditorUtility.SetDirty(urpCam);

            return true;
        }

        void UpgradeGameObject(GameObject go)
        {
            var cam = go.GetComponentInChildren<U2DPackage.PixelPerfectCamera>();

            if (cam != null)
                UpgradePixelPerfectCamera(cam);
        }

#else
        public bool isEnabled => false;
        public string isDisabledMessage => "Pixel Perfect package is not installed. Please install the Pixel Perfect package to enable this converter.";
#endif

        public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
        {
#if PIXEL_PERFECT_2D_EXISTS
            var returnList = new List<IRenderPipelineConverterItem>();
            void OnSearchFinish()
            {
                onScanFinish?.Invoke(returnList);
            }

            var processedIds = new HashSet<string>();

            SearchServiceUtils.RunQueuedSearch
            (
                SearchServiceUtils.IndexingOptions.DeepSearch,
                new List<(string query, string description)> { ("t:UnityEngine.U2D.PixelPerfectCamera", "Game Objects Referencing a U2D Pixel Perfect Camera") },
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

                    var go = unityObject as GameObject;
                    var ppCameraItem = new PixelPerfectCameraConverterItem(gid.ToString())
                    {
                        name = $"{unityObject.name} ({(type == 1 ? "Prefab" : "SceneObject")})",
                        info = type == 1 ? AssetDatabase.GetAssetPath(unityObject) : go.scene.path,
                    };

                    returnList.Add(ppCameraItem);
                },
                OnSearchFinish
            );
#else
            throw new InvalidOperationException();
#endif
        }

        public Status Convert(IRenderPipelineConverterItem item, out string message)
        {
            message = string.Empty;

#if PIXEL_PERFECT_2D_EXISTS
            if (item is PixelPerfectCameraConverterItem ppCameraItem)
            {
                if (ppCameraItem.type == 1) URP2DConverterUtility.UpgradePrefab(ppCameraItem.info, UpgradeGameObject);
                else URP2DConverterUtility.UpgradeScene(ppCameraItem.info, UpgradeGameObject);

                return Status.Success;
            }
#endif
            return Status.Error;
        }
    }
}
