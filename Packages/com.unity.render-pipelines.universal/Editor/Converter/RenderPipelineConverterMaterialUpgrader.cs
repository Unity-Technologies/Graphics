using System.Collections.Generic;
using System.Text;
using UnityEditor.Rendering.Converter;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    internal abstract class RenderPipelineConverterMaterialUpgrader : RenderPipelineAssetsConverter
    {
        /// <summary>
        /// List of material upgraders to use for this converter.
        /// </summary>
        protected abstract List<MaterialUpgrader> upgraders { get; }

        protected override List<(string query, string description)> contextSearchQueriesAndIds
        {
            get
            {
                List<(string materialName, string searchQuery)> list = new();
                foreach (var upgrader in m_UpgradersCache)
                {
                    var shader = Shader.Find(upgrader.OldShaderPath);
                    if (shader == null)
                    {
                        Debug.LogWarning($"Shader '{upgrader.OldShaderPath}' not found for upgrader {upgrader.GetType().Name}. This may indicate that the shader has been removed or renamed.");
                        continue;
                    }
                    string formattedId = $"<$object:{GlobalObjectId.GetGlobalObjectIdSlow(shader)},UnityEngine.Object$>";
                    list.Add(($"p: t:Material ref={formattedId}", $"{upgrader.OldShaderPath} -> {upgrader.NewShaderPath}"));
                }
                return list;
            }
        }

        private List<MaterialUpgrader> m_UpgradersCache;

        public RenderPipelineConverterMaterialUpgrader()
        {
            m_UpgradersCache = upgraders;

            if (m_UpgradersCache.Count == 0)
            {
                Debug.Log($"No upgraders specified for this converter ({GetType()}). Skipping Initialization.");
                return;
            }
        }

        protected override Status ConvertObject(UnityEngine.Object obj, StringBuilder message)
        {
            if (obj is not Material mat)
            {
                message.AppendLine("Object is not a Material.");
                return Status.Error;
            }

            string upgradingMessage = string.Empty;
            if(!MaterialUpgrader.Upgrade(mat, upgraders, MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound, ref upgradingMessage))
            {
                message.AppendLine($"Material upgrade failed: {upgradingMessage}");
                return Status.Error;
            }

            return Status.Success;
        }

    }
}
