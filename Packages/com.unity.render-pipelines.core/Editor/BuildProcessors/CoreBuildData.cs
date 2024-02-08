using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Contains a set of needed data for building.
    /// This might also being called when building Asset Bundles
    /// </summary>
    public class CoreBuildData : IDisposable
    {
        static CoreBuildData m_Instance = null;

        /// <summary>
        /// Instance to the current <see cref="CoreBuildData"/>.
        /// </summary>
        public static CoreBuildData instance => m_Instance ??= CreateInstance();

        /// <summary>
        /// If the target build has an SRP configured
        /// </summary>
        public bool buildingPlayerForRenderPipeline { get; private set; } = false;

        /// <summary>
        /// A valid type of <see cref="RenderPipelineAsset"/>, that the build is targeting,
        /// </summary>
        public Type currentRenderPipelineAssetType { get; private set; } = null;

        /// <summary>
        /// A list of <see cref="RenderPipelineAsset"/>, all of them of the same type.
        /// </summary>
        public List<RenderPipelineAsset> renderPipelineAssets { get; private set; } = new();

        internal Dictionary<int, ComputeShader> computeShaderCache { get; private set; } = new();

        internal bool pipelineSupportGPUResidentDrawer { get; private set; } = false;
        internal bool playerNeedGPUResidentDrawer { get; private set; } = false;

        private CoreBuildData(BuildTarget buildTarget)
        {
            m_Instance = this;

            if (!buildTarget.TryGetRenderPipelineAssets(renderPipelineAssets))
                return;

            buildingPlayerForRenderPipeline = true;

            //We can check only the first as we don't support multiple pipeline type in player
            var asset = renderPipelineAssets[0];
            currentRenderPipelineAssetType = asset.GetType();

            CheckGPUResidentDrawerUsage();
        }

        private static CoreBuildData CreateInstance()
            => new(EditorUserBuildSettings.activeBuildTarget);

        private void CheckGPUResidentDrawerUsage()
        {
            foreach (var renderPipelineAsset in renderPipelineAssets)
            {
                if (renderPipelineAsset is IGPUResidentRenderPipeline gpuResidentPipelineAsset
                    && gpuResidentPipelineAsset.IsGPUResidentDrawerSupportedBySRP())
                {
                    // Record if any pipeline supports the GPU resident drawer
                    pipelineSupportGPUResidentDrawer = true;

                    // If any pipeline already has GPU resident drawer enabled, then record this and also early out
                    if (gpuResidentPipelineAsset.gpuResidentDrawerSettings.mode != GPUResidentDrawerMode.Disabled)
                    {
                        playerNeedGPUResidentDrawer = true;
                        break;
                    }
                }
            }

            if (!playerNeedGPUResidentDrawer)
                return;

            GraphicsSettings.GetRenderPipelineSettings<GPUResidentDrawerResources>()
                .ForEachFieldOfType<ComputeShader>(computeShader => computeShaderCache.Add(computeShader.GetInstanceID(), computeShader));
        }

        /// <summary>
        /// Dispose all the gathered data for building
        /// </summary>
        public void Dispose()
        {
            renderPipelineAssets?.Clear();
            computeShaderCache?.Clear();
            m_Instance = null;
        }
    }
}
