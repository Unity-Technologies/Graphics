using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Converter
{
    /// <summary>
    /// Represents the target for render pipeline configuration
    /// </summary>
    internal static class RenderPipelineTarget
    {
        /// <summary>
        /// Indicates the default render pipeline in Graphics Settings (not tied to a specific quality level)
        /// </summary>
        public const int DefaultGraphicsSettings = -1;
    }

    [Serializable]
    internal class RenderSettingsConverterItem : IRenderPipelineConverterItem
    {
        public int qualityLevelIndex { get; set; }

        public string name { get; set; }

        public string info { get; set; }

        public bool isEnabled { get; set; }
        public string isDisabledMessage { get; set; }

        /// <summary>
        /// Settings path to open when clicked (e.g., "Project/Quality" or "Project/Graphics")
        /// </summary>
        public string settingsPath { get; set; } = "Project/Quality";

        private static Texture2D s_CachedIcon;

        public Texture2D icon
        {
            get
            {
                if (s_CachedIcon == null)
                {
                    var iconAttribute = typeof(RenderPipelineAsset).GetCustomAttribute<IconAttribute>();
                    if (iconAttribute != null && !string.IsNullOrEmpty(iconAttribute.path))
                        s_CachedIcon = EditorGUIUtility.IconContent(iconAttribute.path)?.image as Texture2D;
                }
                return s_CachedIcon;
            }
        }
        public void OnClicked()
        {
            SettingsService.OpenProjectSettings(settingsPath);
        }
    }

    [Serializable]
    abstract class RenderSettingsConverter : IRenderPipelineConverter
    {
        public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
        {
            List<IRenderPipelineConverterItem> renderPipelineConverterItems = new();

            var graphicsItems = new List<IRenderPipelineConverterItem>();
            var defaultRPItem = new RenderSettingsConverterItem
            {
                qualityLevelIndex = RenderPipelineTarget.DefaultGraphicsSettings,
                name = "Default Render Pipeline",
                settingsPath = "Project/Graphics"
            };

            if (GraphicsSettings.defaultRenderPipeline is not RenderPipelineAsset)
            {
                defaultRPItem.isEnabled = true;
                defaultRPItem.info = "Create a default Render Pipeline Asset for Graphics Settings";
            }
            else
            {
                defaultRPItem.info = "Graphics Settings already reference a default Render Pipeline Asset.";
                defaultRPItem.isEnabled = false;
                defaultRPItem.isDisabledMessage = defaultRPItem.info;
            }
            graphicsItems.Add(defaultRPItem);

            var qualityItems = new List<IRenderPipelineConverterItem>();
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
                qualityItems.Add(item);
            });

            var graphicsFolder = new RenderPipelineConverterUtility.AssetGroupItem
            {
                name = "Graphics",
                info = "Default Render Pipeline in Graphics Settings",
                assetType = typeof(RenderPipelineAsset),
                children = graphicsItems
            };

            var qualityFolder = new RenderPipelineConverterUtility.AssetGroupItem
            {
                name = "Quality",
                info = "Render Pipeline Assets per Quality Level",
                assetType = typeof(RenderPipelineAsset),
                children = qualityItems
            };

            renderPipelineConverterItems.Add(graphicsFolder);
            renderPipelineConverterItems.Add(qualityFolder);

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
                    if (qualityLevelItem.qualityLevelIndex == RenderPipelineTarget.DefaultGraphicsSettings)
                    {
                        message = "Default Render Pipeline Asset created and assigned to Graphics Settings.";
                    }
                    else
                    {
                        message = "Each Quality Level now has a new, unique Render Pipeline Asset, but all share identical settings. Modify each asset to restore your performance/quality tiers.";
                    }
                    return Status.Warning;
                }
            }

            return Status.Error;
        }

        private bool CreateRPAssetForQualityLevel(int qualityIndex, out string message)
        {
            bool ok = false;
            message = string.Empty;

            if (qualityIndex == RenderPipelineTarget.DefaultGraphicsSettings)
            {
                if (GraphicsSettings.defaultRenderPipeline is RenderPipelineAsset rpAsset)
                {
                    message = $"Graphics Settings already reference a default Render Pipeline Asset: {rpAsset.name}.";
                }
                else
                {
                    var asset = CreateAsset("DefaultRenderPipeline");

                    if (asset != null)
                    {
                        SetPipelineSettings(asset);
                        EditorUtility.SetDirty(asset);
                        AssetDatabase.SaveAssetIfDirty(asset);

                        GraphicsSettings.defaultRenderPipeline = asset;
                        ok = true;
                    }
                    else
                    {
                        message = "Failed to create Render Pipeline Asset for Graphics Settings.";
                    }
                }

                return ok;
            }

            var currentQualityLevel = QualitySettings.GetQualityLevel();

            QualitySettings.SetQualityLevel(qualityIndex);

            if (QualitySettings.renderPipeline is RenderPipelineAsset qualityRPAsset)
            {
                message = $"Quality Level {qualityIndex} already references a Render Pipeline Asset: {qualityRPAsset.name}.";
            }
            else
            {
                var asset = CreateAsset($"{QualitySettings.names[qualityIndex]}");

                if (asset != null)
                {
                    SetPipelineSettings(asset);
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);

                    QualitySettings.renderPipeline = asset;
                    ok = true;
                }
                else
                {
                    message = "Failed to create Render Pipeline Asset.";
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
