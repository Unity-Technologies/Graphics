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

        /// <summary>
        /// Whether the current build's managed code variant has <c>UNITY_ENABLE_CHECKS</c> defined
        /// (Debug or Checked variant).
        /// </summary>
        /// <remarks>
        /// Use this to gate inclusion of diagnostic shaders, resources, and other build-time decisions
        /// that mirror C# code paths compiled under <c>#if UNITY_ENABLE_CHECKS</c>.
        /// </remarks>
        public bool useDiagnosticChecks { get; private set; } = false;

        /// <summary>
        /// Obsolete. Always returns <c>false</c>.
        /// </summary>
        [Obsolete("developmentBuild is no longer populated and always returns false. Use useDiagnosticChecks for stripping decisions that mirror UNITY_ENABLE_CHECKS-gated C# code.")]
        public bool developmentBuild => false;

        internal Dictionary<EntityId, ComputeShader> computeShaderCache { get; private set; } = new();

        internal bool pipelineSupportGPUResidentDrawer { get; private set; } = false;
        internal bool playerNeedGPUResidentDrawer { get; private set; } = false;

        private CoreBuildData(BuildTarget buildTarget)
        {
            m_Instance = this;

            useDiagnosticChecks = PlayerSettings.GetManagedCodeVariant(GetNamedBuildTarget(buildTarget)) <= ManagedCodeVariant.Checked;

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

        // NamedBuildTarget for buildTarget under the active subtarget (Player vs Server for Standalone).
        private static NamedBuildTarget GetNamedBuildTarget(BuildTarget buildTarget)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            if (buildTargetGroup == BuildTargetGroup.Standalone &&
                EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
            {
                return NamedBuildTarget.Server;
            }
            return NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        }

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

            #pragma warning disable 618 // Todo(@daniel.andersen): Remove deprecated API usage
            GraphicsSettings.GetRenderPipelineSettings<GPUResidentDrawerResources>()
                .ForEachFieldOfType<ComputeShader>(computeShader => computeShaderCache.Add(computeShader.GetEntityId(), computeShader));
            #pragma warning restore 618
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
