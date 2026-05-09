using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class URPBuildData : IDisposable
    {
        // Instance can be null when building AssetBundles, otherwise the lifetime will be given by URPPreprocessBuild
        static URPBuildData m_Instance = null;

        public static URPBuildData instance => m_Instance ??= new(EditorUserBuildSettings.activeBuildTarget);

        public bool buildingPlayerForUniversalRenderPipeline { get; private set; }

        public List<UniversalRenderPipelineAsset> renderPipelineAssets { get; private set; } = new List<UniversalRenderPipelineAsset>();

        public List<ScriptableRendererData> rendererDataList { get; private set; } = new List<ScriptableRendererData>();

        public URPBuildData(BuildTarget buildTarget)
        {
            m_Instance = this;

            buildingPlayerForUniversalRenderPipeline = CoreBuildData.instance.buildingPlayerForRenderPipeline &&
                                                       CoreBuildData.instance.currentRenderPipelineAssetType == typeof(UniversalRenderPipelineAsset);

            if (buildingPlayerForUniversalRenderPipeline)
            {
                foreach (var asset in CoreBuildData.instance.renderPipelineAssets)
                {
                    Assert.IsTrue(asset is UniversalRenderPipelineAsset);
                    renderPipelineAssets.Add(asset as UniversalRenderPipelineAsset);
                }

                foreach (var asset in renderPipelineAssets)
                {
                    foreach (var rendererData in asset.m_RendererDataList)
                    {
                        if (rendererData != null)
                            rendererDataList.Add(rendererData);
                    }
                }
            }
        }

        public void Dispose()
        {
            renderPipelineAssets?.Clear();
            rendererDataList?.Clear();

            buildingPlayerForUniversalRenderPipeline = false;

            CoreBuildData.instance.Dispose();

            m_Instance = null;
        }
    }
}
