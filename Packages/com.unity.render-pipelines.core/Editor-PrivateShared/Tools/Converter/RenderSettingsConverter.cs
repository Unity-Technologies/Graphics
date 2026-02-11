using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Converter
{
    [Serializable]
    internal class RenderSettingsConverterItem : IRenderPipelineConverterItem
    {
        public int qualityLevelIndex { get; set; }

        public string name { get; set; }

        public string info { get; set; }

        public bool isEnabled { get; set; }
        public string isDisabledMessage { get; set; }

        public Texture2D icon
        {
            get
            {
                var iconAttribute = typeof(RenderPipelineAsset).GetCustomAttribute<IconAttribute>();
                if (iconAttribute == null || string.IsNullOrEmpty(iconAttribute.path))
                    return null;
                return EditorGUIUtility.IconContent(iconAttribute.path)?.image as Texture2D;
            }
        }
        public void OnClicked()
        {
            SettingsService.OpenProjectSettings("Project/Quality");
        }
    }

    [Serializable]
    abstract class RenderSettingsConverter : IRenderPipelineConverter
    {
        public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
        {
            List<IRenderPipelineConverterItem> renderPipelineConverterItems = new ();
            QualitySettings.ForEach((index, name) =>
            {
                var item = new RenderSettingsConverterItem
                {
                    qualityLevelIndex = index,
                    name = name
                };

                if (QualitySettings.renderPipeline is not RenderPipelineAsset)
                {
                    item.isEnabled = true;
                    item.info = $"Create a Render Pipeline Asset for Quality Level {index} ({name})";
                }
                else
                {
                    item.info = "Quality Level already references a Render Pipeline Asset.";
                    item.isEnabled = false;
                    item.isDisabledMessage = item.info;
                }
                renderPipelineConverterItems.Add(item);
            });

            onScanFinish?.Invoke(renderPipelineConverterItems);
        }
        public abstract bool isEnabled { get; }

        public abstract string isDisabledMessage { get; }

        public Status Convert(IRenderPipelineConverterItem item, out string message)
        {
            message = string.Empty;

            if (item is RenderSettingsConverterItem qualityLevelItem)
            {
                if (CreateRPAssetForQualityLevel(qualityLevelItem.qualityLevelIndex, out message))
                {
                    message = "Each Quality Level now has a new, unique RP asset, but all share identical settings. Modify each asset to restore your performance/quality tiers.";
                    return Status.Warning;
                }
            }

            return Status.Error;
        }

        private bool CreateRPAssetForQualityLevel(int qualityIndex, out string message)
        {
            bool ok = false;
            message = string.Empty;

            var currentQualityLevel = QualitySettings.GetQualityLevel();

            QualitySettings.SetQualityLevel(qualityIndex);

            if (QualitySettings.renderPipeline is RenderPipelineAsset rpAsset)
            {
                message = $"Quality Level {qualityIndex} already references a Render Pipeline Asset: {rpAsset.name}.";
            }
            else
            {
                var asset = CreateAsset($"{QualitySettings.names[qualityIndex]}");

                if (asset != null)
                {
                    // Map built-in data to the URP asset data
                    SetPipelineSettings(asset);

                    // Set the asset dirty to make sure that the renderer data is saved
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);

                    QualitySettings.renderPipeline = asset;
                    ok = true;
                }
                else
                {
                    message = "Failed to create Universal Render Pipeline Asset.";
                }
            }

            // Restore back the quality level
            QualitySettings.SetQualityLevel(currentQualityLevel);

            return ok;
        }

        protected abstract RenderPipelineAsset CreateAsset(string name);
        
        protected abstract void SetPipelineSettings(RenderPipelineAsset asset);
    }
}
