using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class CoreBuildData : IDisposable
    {
        static CoreBuildData m_Instance = null;
        public static CoreBuildData instance => m_Instance ??= CreateInstance();

        public bool buildingPlayerForRenderPipeline { get; private set; } = false;
        public List<RenderPipelineAsset> renderPipelineAssets { get; private set; } = new();
        public Dictionary<int, ComputeShader> computeShaderCache { get; private set; } = new();

        public bool pipelineSupportGPUResidentDrawer { get; private set; } = false;
        public bool playerNeedGPUResidentDrawer { get; private set; } = false;

        private CoreBuildData(BuildTarget buildTarget)
        {
            m_Instance = this;

            if (!buildTarget.TryGetRenderPipelineAssets(renderPipelineAssets))
                return;

            buildingPlayerForRenderPipeline = true;

            CheckGPUResidentDrawerUsage();
        }

        public static CoreBuildData CreateInstance()
            => new(EditorUserBuildSettings.activeBuildTarget);

        private void CheckGPUResidentDrawerUsage()
        {
            //We can check only the first as we don't support multiple pipeline type in player
            pipelineSupportGPUResidentDrawer = renderPipelineAssets[0] is IGPUResidentRenderPipeline gpuResidentRenderPipeline && gpuResidentRenderPipeline.IsGPUResidentDrawerSupportedBySRP();
            if (!pipelineSupportGPUResidentDrawer)
                return;

            foreach (IGPUResidentRenderPipeline gpuResidentPipelineAsset in renderPipelineAssets)
                if (gpuResidentPipelineAsset.gpuResidentDrawerSettings.mode != GPUResidentDrawerMode.Disabled)
                {
                    playerNeedGPUResidentDrawer = true;
                    break;
                }

            if (!playerNeedGPUResidentDrawer)
                return;

            GraphicsSettings.GetRenderPipelineSettings<GPUResidentDrawerResources>()
                .ForEachFieldOfType<ComputeShader>(computeShader => computeShaderCache.Add(computeShader.GetInstanceID(), computeShader));
        }

        public void Dispose()
        {
            renderPipelineAssets?.Clear();
            computeShaderCache?.Clear();
            m_Instance = null;
        }
    }
}