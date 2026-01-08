using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Internal targets.
        private RTHandle m_LineColorBufferRT;
        private RTHandle m_LineDepthBufferRT;
        private RTHandle m_LineMVBufferRT;

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

            m_LineColorBufferRT = RTHandles.Alloc(Vector2.one, format:GraphicsFormat.R32G32B32A32_SFloat, slices: TextureXR.slices, dimension: TextureXR.dimension,
                useMipMap: false, autoGenerateMips: false, enableRandomWrite: true, useDynamicScale: true, name: "LineColorBuffer");
            m_LineDepthBufferRT = RTHandles.Alloc(Vector2.one, format: GraphicsFormat.R32_SFloat, slices: TextureXR.slices, dimension: TextureXR.dimension,
                useMipMap: false, autoGenerateMips: false, enableRandomWrite: true, useDynamicScale: true, name: "LineDepthBuffer");
            m_LineMVBufferRT = RTHandles.Alloc(Vector2.one,  format: Builtin.GetMotionVectorFormat(), slices: TextureXR.slices, dimension: TextureXR.dimension,
                useMipMap: false, autoGenerateMips: false, enableRandomWrite: true, useDynamicScale: true, name: "LineMVBuffer");
        }

        void CleanupLineRendering()
        {
            if (!s_SupportLineRendering)
                return;

            CoreUtils.Destroy(m_LineCompositePass);

            m_LineColorBufferRT?.Release();
            m_LineDepthBufferRT?.Release();
            m_LineMVBufferRT?.Release();

            m_LineColorBuffer = TextureHandle.nullHandle;
            m_LineDepthBuffer = TextureHandle.nullHandle;
            m_LineMVBuffer = TextureHandle.nullHandle;

            LineRendering.Instance.Cleanup();
        }

        void ImportLineRenderingTargetsToRenderGraphIfNeeded()
        {
            if (!m_LineColorBuffer.IsValid())
                m_LineColorBuffer = m_RenderGraph.ImportTexture(m_LineColorBufferRT);

            if (!m_LineDepthBuffer.IsValid())
                m_LineDepthBuffer = m_RenderGraph.ImportTexture(m_LineDepthBufferRT);

            if (!m_LineMVBuffer.IsValid())
                m_LineMVBuffer = m_RenderGraph.ImportTexture(m_LineMVBufferRT);
        }

        class LineRendererSetupData
        {
            public BufferHandle lightListCluster;
            public BufferHandle perVoxelOffset;
            public BufferHandle perTileLogBaseTweak;

            public BufferHandle waterLine;
            public BufferHandle cameraHeightBuffer;
            public BufferHandle waterSurfaceProfiles;
            public TextureHandle waterGBuffer3;

            public TextureHandle waterDepthStencil;

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
            public int lineCompositePassColorIndex;
            public int lineCompositePassDepthMovecIndex;
            public int lineCompositePassAllIndex;
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

        void RenderLines(RenderGraph renderGraph, in TextureHandle depthPrepassTexture, HDCamera hdCamera, in TransparentPrepassOutput transparentPrepass, BuildGPULightListOutput lightLists)
        {
            if (!LineRenderingIsEnabled(hdCamera, out var settings))
                return;

            ImportLineRenderingTargetsToRenderGraphIfNeeded();

            // Here we need to bind some SRP-specific buffers and clear the internal targets.
            using (var builder = renderGraph.AddUnsafePass<LineRendererSetupData>("Setup Line Rendering", out var passData, ProfilingSampler.Get(HDProfileId.LineRenderingSetup)))
            {
                passData.lightListCluster    = lightLists.perVoxelLightLists;
                builder.UseBuffer(passData.lightListCluster, AccessFlags.Read);
                passData.perVoxelOffset      = lightLists.perVoxelOffset;
                builder.UseBuffer(passData.perVoxelOffset, AccessFlags.Read);
                passData.perTileLogBaseTweak = lightLists.perTileLogBaseTweak;
                builder.UseBuffer(passData.perTileLogBaseTweak, AccessFlags.Read);

                // Water absorption buffers
                passData.waterDepthStencil = transparentPrepass.depthBufferPreRefraction;
                builder.UseTexture(passData.waterDepthStencil, AccessFlags.Read);
                passData.waterSurfaceProfiles = transparentPrepass.waterSurfaceProfiles;
                builder.UseBuffer(passData.waterSurfaceProfiles, AccessFlags.Read);
                passData.cameraHeightBuffer = transparentPrepass.waterGBuffer.cameraHeight;
                builder.UseBuffer(passData.cameraHeightBuffer, AccessFlags.Read);
                passData.waterLine = transparentPrepass.waterLine;
                builder.UseBuffer(passData.waterLine, AccessFlags.Read);
                passData.waterGBuffer3 = transparentPrepass.waterGBuffer.waterGBuffer3;
                builder.UseTexture(passData.waterGBuffer3, AccessFlags.Read);

                passData.targetColor = m_LineColorBuffer;
                builder.UseTexture(passData.targetColor, AccessFlags.Write);
                passData.targetDepth = m_LineDepthBuffer;
                builder.UseTexture(passData.targetDepth, AccessFlags.Write);
                passData.targetMV    = m_LineMVBuffer;
                builder.UseTexture(passData.targetMV, AccessFlags.Write);

                builder.SetRenderFunc(static (LineRendererSetupData data, UnsafeGraphContext ctx) =>
                {
                    var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                    // Bind the light lists (required for the light loop to work with offscreen shading).
                    {
                        natCmd.SetGlobalBuffer(HDShaderIDs.g_vLightListCluster, data.lightListCluster);

                        // Next two are only for cluster rendering. PerTileLogBaseTweak is only when using depth buffer so can be invalid as well.
                        if (data.perVoxelOffset.IsValid())
                            natCmd.SetGlobalBuffer(HDShaderIDs.g_vLayeredOffsetsBuffer, data.perVoxelOffset);
                        if (data.perTileLogBaseTweak.IsValid())
                            natCmd.SetGlobalBuffer(HDShaderIDs.g_logBaseBuffer, data.perTileLogBaseTweak);

                        CoreUtils.SetKeyword(natCmd, "USE_FPTL_LIGHTLIST", false);
                        CoreUtils.SetKeyword(natCmd, "USE_CLUSTERED_LIGHTLIST", true);
                    }

                    // Water absorption buffers
                    {
                        natCmd.SetGlobalTexture(HDShaderIDs._StencilTexture, data.waterDepthStencil, RenderTextureSubElement.Stencil);
                        natCmd.SetGlobalTexture(HDShaderIDs._RefractiveDepthBuffer, data.waterDepthStencil, RenderTextureSubElement.Depth);

                        natCmd.SetGlobalBuffer(HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);
                        natCmd.SetGlobalBuffer(HDShaderIDs._WaterSurfaceProfiles, data.waterSurfaceProfiles);
                        natCmd.SetGlobalBuffer(HDShaderIDs._WaterLineBuffer, data.waterLine);
                        natCmd.SetGlobalTexture(HDShaderIDs._WaterGBufferTexture3, data.waterGBuffer3);
                    }

                    // Clear the internal targets.
                    CoreUtils.SetRenderTarget(natCmd, data.targetColor, ClearFlag.Color, Color.black);
                    CoreUtils.SetRenderTarget(natCmd, data.targetDepth, ClearFlag.Color, Color.black);
                    CoreUtils.SetRenderTarget(natCmd, data.targetMV,    ClearFlag.Color, Color.clear);
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

        void ComposeLines(RenderGraph renderGraph, HDCamera hdCamera, in TextureHandle colorBuffer, in TextureHandle depthBuffer, in TextureHandle motionVectorBuffer, int compositionMode)
        {
            if (!LineRenderingIsEnabled(hdCamera, out var settings))
                return;

            if (compositionMode != -1 && compositionMode != (int)settings.compositionMode.value)
                return;

            ImportLineRenderingTargetsToRenderGraphIfNeeded();

            using (var builder = renderGraph.AddUnsafePass<LineRendererCompositeData>("Composite Hair", out var passData, ProfilingSampler.Get(HDProfileId.LineRenderingComposite)))
            {
                passData.compositePass   = m_LineCompositePass;

                passData.mainTargetColor = colorBuffer;
                builder.SetRenderAttachment(colorBuffer, 0);
                passData.mainTargetDepth = depthBuffer;
                builder.SetRenderAttachmentDepth(depthBuffer, AccessFlags.ReadWrite);

                if (motionVectorBuffer.IsValid() && hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors))
                {
                    // The motion vectors may be invalid in case of material debug view. So don't bind it in that case.
                    passData.mainTargetMV = motionVectorBuffer;
                    builder.SetRenderAttachment(motionVectorBuffer, 1);
                }
                else
                    passData.mainTargetMV = TextureHandle.nullHandle;

                passData.lineTargetColor = m_LineColorBuffer;
                builder.UseTexture(passData.lineTargetColor, AccessFlags.Read);
                passData.lineTargetDepth = m_LineDepthBuffer;
                builder.UseTexture(passData.lineTargetDepth, AccessFlags.Read);
                passData.lineTargetMV    = m_LineMVBuffer;
                builder.UseTexture(passData.lineTargetMV, AccessFlags.Read);
                passData.writeDepthAndMovecAlphaTreshold = settings.writeDepthAlphaThreshold.value;
                passData.lineCompositePassColorIndex = m_LineCompositePassColorIndex;
                passData.lineCompositePassDepthMovecIndex = m_LineCompositePassDepthMovecIndex;
                passData.lineCompositePassAllIndex = m_LineCompositePassAllIndex;

                builder.SetRenderFunc(static (LineRendererCompositeData passData, UnsafeGraphContext ctx) =>
                {
                    var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    passData.compositePass.SetTexture(HDShaderIDs._LineColorTexture,  passData.lineTargetColor);
                    passData.compositePass.SetTexture(HDShaderIDs._LineDepthTexture,  passData.lineTargetDepth);
                    passData.compositePass.SetTexture(HDShaderIDs._LineMotionTexture, passData.lineTargetMV);
                    passData.compositePass.SetFloat(HDShaderIDs._LineAlphaDepthWriteThreshold, passData.writeDepthAndMovecAlphaTreshold );
                    if (passData.writeDepthAndMovecAlphaTreshold > 0)
                    {
                        HDUtils.DrawFullScreen(natCmd, passData.compositePass, new RenderTargetIdentifier[] { passData.mainTargetColor}, passData.mainTargetDepth, null, passData.lineCompositePassColorIndex); //color composite
                        HDUtils.DrawFullScreen(natCmd, passData.compositePass, new RenderTargetIdentifier[] { passData.mainTargetMV }, passData.mainTargetDepth, null, passData.lineCompositePassDepthMovecIndex); //depth & movec composite
                    }
                    else
                    {
                        HDUtils.DrawFullScreen(natCmd, passData.compositePass, new RenderTargetIdentifier[] { passData.mainTargetColor, passData.mainTargetMV }, passData.mainTargetDepth, null, passData.lineCompositePassAllIndex); //composite all
                    }
                });
            }
        }
    }
}
