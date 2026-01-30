using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Rendering.Converter;
using UnityEngine.Categorization;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [PipelineTools]
    [ElementInfo(Name = "Photoshop Assets Upgrader",
             Order = 400,
             Description = "This converter reimports the assets with extension .psd and .psb.")]
    internal sealed class PhotoshopAssetsUpgrader : IRenderPipelineConverter
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

            SearchServiceUtils.RunQueuedSearch
            (
                SearchServiceUtils.IndexingOptions.DeepSearch,
                new List<(string, string)>()
                {
                    ("p: ext:psd", "PSD"),
                    ("p: ext:psb", "PSB")
                },
                (item, description) =>
                {
                    var assetItem = new RenderPipelineConverterAssetItem(item.id)
                    {
                        info = description
                    };
                    returnList.Add(assetItem);
                },
                OnSearchFinish
            );
        }

        public Status Convert(IRenderPipelineConverterItem item, out string message)
        {
            var assetItem = item as RenderPipelineConverterAssetItem;

            var obj = assetItem.LoadObject();

            if (obj == null)
            {
                message = $"Failed to load {assetItem.name} Global ID {assetItem.guid} Asset Path {assetItem.assetPath}";
                return Status.Error;
            }
            
            URP2DConverterUtility.UpgradePSB(assetItem.assetPath);
            message = string.Empty;
            return Status.Success;
        }
    }
}
