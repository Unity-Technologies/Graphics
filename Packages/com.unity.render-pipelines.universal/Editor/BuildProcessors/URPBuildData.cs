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

        public static URPBuildData instance => m_Instance ??= new(EditorUserBuildSettings.activeBuildTarget, Debug.isDebugBuild);

        public bool buildingPlayerForUniversalRenderPipeline { get; private set; }

        public List<UniversalRenderPipelineAsset> renderPipelineAssets { get; private set; } = new List<UniversalRenderPipelineAsset>();

        public URPBuildData(BuildTarget buildTarget, bool isDevelopmentBuild)
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
            }
        }

        public void Dispose()
        {
            renderPipelineAssets?.Clear();

            buildingPlayerForUniversalRenderPipeline = false;

            CoreBuildData.instance.Dispose();

            m_Instance = null;
        }
    }
}
