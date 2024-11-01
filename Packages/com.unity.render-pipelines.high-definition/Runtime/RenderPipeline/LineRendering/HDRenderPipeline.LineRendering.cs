using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Internal targets.
        private TextureHandle m_LineColorBuffer;
        private TextureHandle m_LineDepthBuffer;
        private TextureHandle m_LineMVBuffer;

        // Compute Utility
        private GPUSort      m_Sorter;
        private GPUPrefixSum m_PrefixSum;

        // Misc.
        private Material m_LineCompositePass;
        private int m_LineCompositePassAllIndex;
        private int m_LineCompositePassColorIndex;
        private int m_LineCompositePassDepthMovecIndex;
        private static bool s_SupportLineRendering;

        void InitializeLineRendering()
        {
            s_SupportLineRendering = asset.currentPlatformRenderPipelineSettings.supportHighQualityLineRendering;

            if (!s_SupportLineRendering)
                return;

            m_LineCompositePass = CoreUtils.CreateEngineMaterial(runtimeShaders.lineCompositePS);
            m_LineCompositePassAllIndex = m_LineCompositePass.FindPass("CompositeAll");
            m_LineCompositePassColorIndex = m_LineCompositePass.FindPass("CompositeColorOnly");
            m_LineCompositePassDepthMovecIndex = m_LineCompositePass.FindPass("CompositeDepthMovecOnly");

            m_PrefixSum = new GPUPrefixSum(new GPUPrefixSum.SystemResources
            {
                computeAsset = runtimeShaders.gpuPrefixSumCS
            });

            m_Sorter = new GPUSort(new GPUSort.SystemResources
            {
                computeAsset = runtimeShaders.gpuSortCS
            });

            LineRendering.Instance.Initialize(new LineRendering.SystemResources
            {
                // Due to a lack of a "Core Resource" concept, we pass along the kernel assets as initialization parameters.
                stagePrepareCS      = runtimeShaders.lineStagePrepareCS,
                stageSetupSegmentCS = runtimeShaders.lineStageSetupSegmentCS,
                stageShadingSetupCS = runtimeShaders.lineStageShadingSetupCS,
                stageRasterBinCS    = runtimeShaders.lineStageRasterBinCS,
                stageWorkQueue      = runtimeShaders.lineStageWorkQueueCS,
                stageRasterFineCS   = runtimeShaders.lineStageRasterFineCS,

                // Misc. Compute Utility
                gpuSort      = m_Sorter,
                gpuPrefixSum = m_PrefixSum
            });

            TextureHandle CreateInternalTarget(GraphicsFormat format, Color clearValue, string name)
            {
                TextureDesc td = new TextureDesc(Vector2.one, true, true);
                {
                    td.format = format;
                    td.useMipMap = false;
                    td.clearBuffer = true;
                    td.clearColor = clearValue;
                    td.autoGenerateMips = false;
                    td.name = name;
                    td.enableRandomWrite = true;
                }
                return m_RenderGraph.CreateSharedTexture(td);
            }

            m_LineColorBuffer = CreateInternalTarget(GraphicsFormat.R32G32B32A32_SFloat, Color.black, "LineColorBuffer");
            m_LineDepthBuffer = CreateInternalTarget(GraphicsFormat.R32_SFloat,          Color.black, "LineDepthBuffer");
            m_LineMVBuffer    = CreateInternalTarget(Builtin.GetMotionVectorFormat(),    Color.clear, "LineMVBuffer");
        }

        void CleanupLineRendering()
        {
            if (!s_SupportLineRendering)
                return;

            CoreUtils.Destroy(m_LineCompositePass);

            m_RenderGraph.ReleaseSharedTexture(m_LineColorBuffer);
            m_RenderGraph.ReleaseSharedTexture(m_LineDepthBuffer);
            m_RenderGraph.ReleaseSharedTexture(m_LineMVBuffer);

            LineRendering.Instance.Cleanup();
        }

        class LineRendererSetupData
        {
            public BufferHandle lightListCluster;
            public BufferHandle perVoxelOffset;
            public BufferHandle perTileLogBaseTweak;

            public TextureHandle targetColor;
            public TextureHandle targetDepth;
            public TextureHandle targetMV;
        }

        class LineRendererCompositeData
        {
            public Material compositePass;

            public TextureHandle mainTargetColor;
            public TextureHandle mainTargetDepth;
            public TextureHandle mainTargetMV;

            public TextureHandle lineTargetColor;
            public TextureHandle lineTargetDepth;
            public TextureHandle lineTargetMV;

            public float writeDepthAndMovecAlphaTreshold;
        }

        internal static bool LineRenderingIsEnabled(HDCamera hdCamera, out HighQualityLineRenderingVolumeComponent settings)
        {
            settings = hdCamera.volumeStack.GetComponent<HighQualityLineRenderingVolumeComponent>();

            if (!s_SupportLineRendering)
                return false;

            if (!settings.enable.value)
                return false;

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.HighQualityLineRendering))
                return false;

#if UNITY_EDITOR
            // TODO: This is a temporary fix to disable hq line rendering on preview cameras until the following error is fixed:
            // PPtr cast failed when dereferencing! Casting from Texture2DArray to RenderTexture!
            if (hdCamera.camera.cameraType == CameraType.Preview)
                return false;
#endif

            return true;
        }

        void RenderLines(RenderGraph renderGraph, TextureHandle depthPrepassTexture, HDCamera hdCamera, BuildGPULightListOutput lightLists)
        {
            if (!LineRenderingIsEnabled(hdCamera, out var settings))
                return;

            // Here we need to bind some SRP-specific buffers and clear the internal targets.
            using (var builder = renderGraph.AddRenderPass<LineRendererSetupData>("Setup Line Rendering", out var passData, ProfilingSampler.Get(HDProfileId.LineRenderingSetup)))
            {
                passData.lightListCluster    = builder.ReadBuffer(lightLists.perVoxelLightLists);
                passData.perVoxelOffset      = builder.ReadBuffer(lightLists.perVoxelOffset);
                passData.perTileLogBaseTweak = builder.ReadBuffer(lightLists.perTileLogBaseTweak);

                passData.targetColor = builder.WriteTexture(m_LineColorBuffer);
                passData.targetDepth = builder.WriteTexture(m_LineDepthBuffer);
                passData.targetMV    = builder.WriteTexture(m_LineMVBuffer);

                builder.SetRenderFunc((LineRendererSetupData data, RenderGraphContext context) =>
                {
                    // Bind the light lists (required for the light loop to work with offscreen shading).
                    {
                        context.cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListCluster, data.lightListCluster);

                        // Next two are only for cluster rendering. PerTileLogBaseTweak is only when using depth buffer so can be invalid as well.
                        if (data.perVoxelOffset.IsValid())
                            context.cmd.SetGlobalBuffer(HDShaderIDs.g_vLayeredOffsetsBuffer, data.perVoxelOffset);
                        if (data.perTileLogBaseTweak.IsValid())
                            context.cmd.SetGlobalBuffer(HDShaderIDs.g_logBaseBuffer, data.perTileLogBaseTweak);

                        CoreUtils.SetKeyword(context.cmd, "USE_FPTL_LIGHTLIST", false);
                        CoreUtils.SetKeyword(context.cmd, "USE_CLUSTERED_LIGHTLIST", true);
                    }

                    // Clear the internal targets.
                    CoreUtils.SetRenderTarget(context.cmd, data.targetColor, ClearFlag.Color, Color.black);
                    CoreUtils.SetRenderTarget(context.cmd, data.targetDepth, ClearFlag.Color, Color.black);
                    CoreUtils.SetRenderTarget(context.cmd, data.targetMV,    ClearFlag.Color, Color.clear);
                });
            }

            var systemSettings = new LineRendering.SystemSettings
            {
                clusterCount         = settings.clusterCount.value,
                compositionMode      = settings.compositionMode.value,
                sortingQuality       = settings.sortingQuality.value,
                tileOpacityThreshold = settings.tileOpacityThreshold.value,
                executeAsync         = hdCamera.frameSettings.HighQualityLinesRunsAsync(),
                memoryBudget         = asset.currentPlatformRenderPipelineSettings.highQualityLineRenderingMemoryBudget,

                // Patch in the debug mode to request from the line renderer (if any).
                debugMode = debugDisplaySettings.data.fullScreenDebugMode != FullScreenDebugMode.HighQualityLines ? -1 : (int)debugDisplaySettings.data.lineRenderingDebugMode
            };

            var targets = new LineRendering.RenderTargets
            {
                color  = m_LineColorBuffer,
                depth  = m_LineDepthBuffer,
                motion = m_LineMVBuffer
            };

            LineRendering.Instance.Draw(new LineRendering.Arguments
            {
                camera         = hdCamera.camera,
                cameraPosition = hdCamera.camera.transform.position,
                cameraFrustum  = hdCamera.frustum,
                renderGraph    = renderGraph,
                depthTexture   = depthPrepassTexture,
                settings       = systemSettings,
                shadingAtlas   = LineRendering.Instance.GetShadingAtlas(renderGraph, hdCamera.camera),
                viewport       = new Vector2(hdCamera.actualWidth, hdCamera.actualHeight),
                matrixIVP      = hdCamera.mainViewConstants.nonJitteredViewProjMatrix.inverse,
                targets        = targets,
                viewCount = hdCamera.viewCount
            });

            PushFullScreenDebugTexture(renderGraph, m_LineColorBuffer, FullScreenDebugMode.HighQualityLines);
        }

        void ComposeLines(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle motionVectorBuffer, int compositionMode)
        {
            if (!LineRenderingIsEnabled(hdCamera, out var settings))
                return;

            if (compositionMode != -1 && compositionMode != (int)settings.compositionMode.value)
                return;

            using (var builder = renderGraph.AddRenderPass<LineRendererCompositeData>("Composite Hair", out var passData, ProfilingSampler.Get(HDProfileId.LineRenderingComposite)))
            {
                passData.compositePass   = m_LineCompositePass;

                passData.mainTargetColor = builder.UseColorBuffer(colorBuffer, 0);
                passData.mainTargetDepth = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                if (motionVectorBuffer.IsValid() && hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors))
                {
                    // The motion vectors may be invalid in case of material debug view. So don't bind it in that case.
                    passData.mainTargetMV = builder.UseColorBuffer(motionVectorBuffer, 1);
                }
                else
                    passData.mainTargetMV = TextureHandle.nullHandle;

                passData.lineTargetColor = builder.ReadTexture(m_LineColorBuffer);
                passData.lineTargetDepth = builder.ReadTexture(m_LineDepthBuffer);
                passData.lineTargetMV    = builder.ReadTexture(m_LineMVBuffer);
                passData.writeDepthAndMovecAlphaTreshold = settings.writeDepthAlphaThreshold.value;

                builder.SetRenderFunc((LineRendererCompositeData passData, RenderGraphContext context) =>
                {
                    passData.compositePass.SetTexture(HDShaderIDs._LineColorTexture,  passData.lineTargetColor);
                    passData.compositePass.SetTexture(HDShaderIDs._LineDepthTexture,  passData.lineTargetDepth);
                    passData.compositePass.SetTexture(HDShaderIDs._LineMotionTexture, passData.lineTargetMV);
                    passData.compositePass.SetFloat(HDShaderIDs._LineAlphaDepthWriteThreshold, passData.writeDepthAndMovecAlphaTreshold );
                    if (passData.writeDepthAndMovecAlphaTreshold > 0)
                    {
                        HDUtils.DrawFullScreen(context.cmd, passData.compositePass, new RenderTargetIdentifier[] { passData.mainTargetColor}, passData.mainTargetDepth, null, m_LineCompositePassColorIndex); //color composite
                        HDUtils.DrawFullScreen(context.cmd, passData.compositePass, new RenderTargetIdentifier[] { passData.mainTargetMV }, passData.mainTargetDepth, null, m_LineCompositePassDepthMovecIndex); //depth & movec composite
                    }
                    else
                    {
                        HDUtils.DrawFullScreen(context.cmd, passData.compositePass, new RenderTargetIdentifier[] { passData.mainTargetColor, passData.mainTargetMV }, passData.mainTargetDepth, null, m_LineCompositePassAllIndex); //composite all
                    }

                });
            }
        }
    }
}
