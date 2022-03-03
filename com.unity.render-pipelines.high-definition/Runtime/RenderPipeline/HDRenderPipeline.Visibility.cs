using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        struct VBufferOutput
        {
            public bool valid;
            public TextureHandle vbuffer0;
            public TextureHandle vbuffer1;
            public TextureHandle vbufferMaterialDepth;
            public VisibilityBRGBindingData visibilityBindingData;

            public static VBufferOutput NewDefault()
            {
                return new VBufferOutput()
                {
                    valid = false,
                    vbuffer0 = TextureHandle.nullHandle,
                    vbuffer1 = TextureHandle.nullHandle,
                    vbufferMaterialDepth = TextureHandle.nullHandle,
                    visibilityBindingData = VisibilityBRGBindingData.NewDefault()
                };
            }

            public VBufferOutput Read(RenderGraphBuilder builder, bool readMaterialDepth = false)
            {
                VBufferOutput readVBuffer = VBufferOutput.NewDefault();
                if (!valid)
                    return readVBuffer;

                readVBuffer.valid = valid;
                readVBuffer.vbuffer0 = builder.ReadTexture(vbuffer0);
                readVBuffer.vbuffer1 = builder.ReadTexture(vbuffer1);
                if (readMaterialDepth)
                    readVBuffer.vbufferMaterialDepth = builder.ReadTexture(vbufferMaterialDepth);
                else
                    readVBuffer.vbufferMaterialDepth = vbufferMaterialDepth;
                readVBuffer.visibilityBindingData = visibilityBindingData;
                return readVBuffer;
            }
        }

        internal bool IsVisibilityPassEnabled()
        {
            return currentAsset != null && currentAsset.VisibilityMaterial != null;
        }

        class VBufferPassData
        {
            public FrameSettings frameSettings;
            public RendererListHandle rendererList;
            public VisibilityBRGBindingData visibilityBindingData;
        }

        void RenderVBuffer(RenderGraph renderGraph, TextureHandle colorBuffer, HDCamera hdCamera, CullingResults cull, ref PrepassOutput output)
        {
            output.vbuffer = VBufferOutput.NewDefault();

            var visibilityBindingData = RenderBRG.GetVisiblityBindingData();
            if (!IsVisibilityPassEnabled() || !visibilityBindingData.valid)
            {
                output.vbuffer.vbuffer0 = renderGraph.defaultResources.blackUIntTextureXR;
                output.vbuffer.vbuffer1 = renderGraph.defaultResources.blackUIntTextureXR;
                output.vbuffer.vbufferMaterialDepth = renderGraph.defaultResources.blackUIntTextureXR;
                return;
            }

            var visibilityMaterial = currentAsset.VisibilityMaterial;
            var visFormat0 = GraphicsFormat.R32_UInt;
            var visFormat1 = GraphicsFormat.R8G8_UInt;
            output.vbuffer.valid = true;

            TextureHandle vbuffer0, vbuffer1;
            using (var builder = renderGraph.AddRenderPass<VBufferPassData>("VBuffer", out var passData, ProfilingSampler.Get(HDProfileId.VBuffer)))
            {
                builder.AllowRendererListCulling(false);

                FrameSettings frameSettings = hdCamera.frameSettings;

                passData.frameSettings = frameSettings;

                output.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                vbuffer0 = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true)
                    {
                        colorFormat = visFormat0,
                        clearBuffer = true,//TODO: for now clear
                        clearColor = Color.clear,
                        name = "VisibilityBuffer0"
                    }), 0);
                vbuffer1 = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true)
                    {
                        colorFormat = visFormat1,
                        clearColor = Color.clear,
                        name = "VisibilityBuffer1"
                    }), 1);

                passData.visibilityBindingData = visibilityBindingData;
                passData.rendererList = builder.UseRendererList(
                   renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(
                        cull, hdCamera.camera,
                        HDShaderPassNames.s_VBufferName,
                        m_CurrentRendererConfigurationBakedLighting,
                        new RenderQueueRange() { lowerBound = (int)HDRenderQueue.Priority.Visibility, upperBound = (int)(int)HDRenderQueue.Priority.Visibility })));

                builder.SetRenderFunc(
                    (VBufferPassData data, RenderGraphContext context) =>
                    {
                        data.visibilityBindingData.globalGeometryPool.BindResourcesGlobal(context.cmd);
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);
                    });
            }

            output.vbuffer.valid = true;
            output.vbuffer.vbuffer0 = vbuffer0;
            output.vbuffer.vbuffer1 = vbuffer1;
            output.vbuffer.vbufferMaterialDepth = RenderMaterialDepth(renderGraph, hdCamera, colorBuffer);
            output.vbuffer.visibilityBindingData = visibilityBindingData;
        }

        static void BindVBufferResources(Material material, in VBufferOutput resources)
        {
            material.SetTexture(HDShaderIDs._VisBufferTexture0, resources.vbuffer0);
            material.SetTexture(HDShaderIDs._VisBufferTexture1, resources.vbuffer1);
            material.SetBuffer(HDShaderIDs._GlobalVisibleClusters, resources.visibilityBindingData.visibleClustersBuffer);
            resources.visibilityBindingData.globalGeometryPool.BindResources(material);
        }

        static void BindVBufferResourcesCS(CommandBuffer cmd, ComputeShader cs, int kernel, in VBufferOutput resources)
        {
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferTexture0, resources.vbuffer0);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferTexture1, resources.vbuffer1);
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._GlobalVisibleClusters, resources.visibilityBindingData.visibleClustersBuffer);
            resources.visibilityBindingData.globalGeometryPool.BindResourcesCS(cmd, cs, kernel);
        }

        static void BindVBufferResourcesGlobal(CommandBuffer cmd, in VBufferOutput resources)
        {
            cmd.SetGlobalTexture(HDShaderIDs._VisBufferTexture0, resources.vbuffer0);
            cmd.SetGlobalTexture(HDShaderIDs._VisBufferTexture1, resources.vbuffer1);
            cmd.SetGlobalBuffer(HDShaderIDs._GlobalVisibleClusters, resources.visibilityBindingData.visibleClustersBuffer);
            resources.visibilityBindingData.globalGeometryPool.BindResourcesGlobal(cmd);
        }

        static void BindVBufferResources(Material material, in VBufferInformation vBufferInfo)
        {
            BindVBufferResources(material, vBufferInfo.vBufferResources);
            material.SetTexture(HDShaderIDs._VisBufferFeatureTiles, vBufferInfo.featureTileClassification);
            material.SetTexture(HDShaderIDs._VisBufferMaterialTiles, vBufferInfo.materialTileClassification);
            material.SetTexture(HDShaderIDs._VisBufferBucketTiles, vBufferInfo.materialBucketID);
        }

        static void BindVBufferResourcesCS(CommandBuffer cmd, ComputeShader cs, int kernel, in VBufferInformation vBufferInfo)
        {
            BindVBufferResourcesCS(cmd, cs, kernel, vBufferInfo.vBufferResources);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferFeatureTiles, vBufferInfo.featureTileClassification);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferMaterialTiles, vBufferInfo.materialTileClassification);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferBucketTiles, vBufferInfo.materialBucketID);
        }

        static void BindVBufferResourcesGlobal(CommandBuffer cmd, in VBufferInformation vBufferInfo)
        {
            BindVBufferResourcesGlobal(cmd, vBufferInfo.vBufferResources);
            cmd.SetGlobalTexture(HDShaderIDs._VisBufferFeatureTiles, vBufferInfo.featureTileClassification);
            cmd.SetGlobalTexture(HDShaderIDs._VisBufferMaterialTiles, vBufferInfo.materialTileClassification);
            cmd.SetGlobalTexture(HDShaderIDs._VisBufferBucketTiles, vBufferInfo.materialBucketID);
        }

        class VBufferMaterialDepthPassData
        {
            public TextureHandle outputDepthBuffer;
            public TextureHandle dummyColorOutput;
            public Material createMaterialDepthMaterial;
        }

        TextureHandle RenderMaterialDepth(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer)
        {
            if (!IsVisibilityPassEnabled() || currentAsset.CreateMaterialDepthMaterial == null)
                return TextureHandle.nullHandle;

            var outputDepth = CreateDepthBuffer(renderGraph, true, hdCamera.msaaSamples);
            using (var builder = renderGraph.AddRenderPass<VBufferMaterialDepthPassData>("Create Vis Buffer Material Depth", out var passData, ProfilingSampler.Get(HDProfileId.VBufferMaterialDepth)))
            {
                passData.outputDepthBuffer = outputDepth;
                passData.createMaterialDepthMaterial = currentAsset.CreateMaterialDepthMaterial;
                passData.dummyColorOutput = builder.WriteTexture(colorBuffer);
                builder.UseDepthBuffer(passData.outputDepthBuffer, DepthAccess.ReadWrite);

                builder.SetRenderFunc(
                    (VBufferMaterialDepthPassData data, RenderGraphContext context) =>
                    {
                        // Doesn't matter what's bound as color buffer
                        HDUtils.DrawFullScreen(context.cmd, passData.createMaterialDepthMaterial, passData.dummyColorOutput, passData.outputDepthBuffer, null, 0);
                    });
            }

            return outputDepth;
        }

        struct VBufferInformation
        {
            public VBufferOutput vBufferResources;
            public TextureHandle featureTileClassification;
            public TextureHandle materialTileClassification;
            public TextureHandle materialBucketID;

            public static VBufferInformation NewDefault()
            {
                return new VBufferInformation()
                {
                    vBufferResources = VBufferOutput.NewDefault()
                };
            }

            public VBufferInformation Read(RenderGraphBuilder builder, bool readMaterialDepth = false)
            {
                var newInfo = VBufferInformation.NewDefault();
                if (!valid)
                    return newInfo;

                newInfo.vBufferResources = vBufferResources.Read(builder);
                newInfo.featureTileClassification = builder.ReadTexture(featureTileClassification);
                newInfo.materialTileClassification = builder.ReadTexture(materialTileClassification);
                newInfo.materialBucketID = builder.ReadTexture(materialBucketID);
                return newInfo;
            }

            public bool valid => vBufferResources.valid;
        }

        VBufferInformation VBufferTileClassification(
            RenderGraph renderGraph, HDCamera hdCamera, ComputeBufferHandle tileFeatureFlags, TextureHandle colorBuffer, VBufferOutput vBufferResources)
        {
            var vBufferInfo = VBufferInformation.NewDefault();
            if (!IsVisibilityPassEnabled())
                return vBufferInfo;

            vBufferInfo.vBufferResources = vBufferResources;
            vBufferInfo.featureTileClassification = VBufferFeatureTileClassification(renderGraph, hdCamera, tileFeatureFlags, colorBuffer);
            VBufferMaterialTileClassification(renderGraph, hdCamera, vBufferResources, out vBufferInfo.materialTileClassification, out vBufferInfo.materialBucketID);
            return vBufferInfo;
        }


        class VBufferTileClassficationData
        {
            public int tileClassSizeX;
            public int tileClassSizeY;
            public ComputeShader createTileClassification;
            public TextureHandle outputTile;
            public ComputeBufferHandle tileFeatureFlagsBuffer;
        }

        TextureHandle VBufferFeatureTileClassification(RenderGraph renderGraph, HDCamera hdCamera, ComputeBufferHandle tileFeatureFlags, TextureHandle colorBuffer)
        {
            if (!IsVisibilityPassEnabled())
                return TextureHandle.nullHandle;

            int tileClassSizeX = HDUtils.DivRoundUp(hdCamera.actualWidth, 64);
            int tileClassSizeY = HDUtils.DivRoundUp(hdCamera.actualHeight, 64);

            var tileClassification = renderGraph.CreateTexture(new TextureDesc(tileClassSizeX, tileClassSizeY, true, true)
            { colorFormat = GraphicsFormat.R32G32_UInt, clearBuffer = true, enableRandomWrite = true, name = "VBufferFeatureTile" });
            using (var builder = renderGraph.AddRenderPass<VBufferTileClassficationData>("Create VBuffer Tiles", out var passData, ProfilingSampler.Get(HDProfileId.VBufferLightTileClassification)))
            {
                passData.outputTile = builder.WriteTexture(tileClassification);

                passData.tileClassSizeX = tileClassSizeX;
                passData.tileClassSizeY = tileClassSizeY;
                passData.createTileClassification = defaultResources.shaders.vbufferTileClassificationCS;
                passData.tileFeatureFlagsBuffer = builder.ReadComputeBuffer(tileFeatureFlags);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc(
                    (VBufferTileClassficationData data, RenderGraphContext context) =>
                    {
                        var cs = data.createTileClassification;
                        var kernel = cs.FindKernel("FeatureTileClassifyReduction");

                        context.cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs.g_TileFeatureFlags, data.tileFeatureFlagsBuffer);
                        context.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferFeatureTileOutput, data.outputTile);

                        int dispatchX = HDUtils.DivRoundUp(data.tileClassSizeX, 8);
                        int dispatchY = HDUtils.DivRoundUp(data.tileClassSizeY, 8);

                        context.cmd.SetComputeVectorParam(cs, HDShaderIDs._VisBufferTileSize, new Vector4(data.tileClassSizeX, data.tileClassSizeY, 0, 0));

                        context.cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, 1);
                    });
            }

            return tileClassification;
        }

        class VBufferMaterialTileClassficationData
        {
            public int tileClassSizeX;
            public int tileClassSizeY;
            public ComputeShader createMaterialTile;
            public TextureHandle outputTile;
            public TextureHandle outputBucketTile;
            public TextureHandle tile8x;
            public TextureHandle bucketTile8x;
            public VBufferOutput vBufferResources;
            public int actualWidth;
            public int actualHeight;
            public ComputeBufferHandle instancedDataBuffer;
        }

        void VBufferMaterialTileClassification(RenderGraph renderGraph, HDCamera hdCamera, in VBufferOutput vBufferResources, out TextureHandle tileClassification, out TextureHandle bucketID)
        {
            if (!IsVisibilityPassEnabled() || !vBufferResources.valid)
            {
                tileClassification = TextureHandle.nullHandle;
                bucketID = TextureHandle.nullHandle;
                return;
            }

            int tileClassSizeX = HDUtils.DivRoundUp(hdCamera.actualWidth, 64);
            int tileClassSizeY = HDUtils.DivRoundUp(hdCamera.actualHeight, 64);

            tileClassification = renderGraph.CreateTexture(new TextureDesc(tileClassSizeX, tileClassSizeY, true, true)
            { colorFormat = GraphicsFormat.R16G16_UInt, clearBuffer = true, enableRandomWrite = true, name = "Material Tile classification" });
            bucketID = renderGraph.CreateTexture(new TextureDesc(tileClassSizeX, tileClassSizeY, true, true)
            { colorFormat = GraphicsFormat.R8_UInt, clearBuffer = true, enableRandomWrite = true, name = "Bucket ID" });

            using (var builder = renderGraph.AddRenderPass<VBufferMaterialTileClassficationData>("Create Material Tile", out var passData, ProfilingSampler.Get(HDProfileId.VBufferMaterialTileClassification)))
            {
                builder.AllowPassCulling(false);

                int tileClassSizeIntermediateX = HDUtils.DivRoundUp(hdCamera.actualWidth, 8);
                int tileClassSizeIntermediateY = HDUtils.DivRoundUp(hdCamera.actualHeight, 8);
                passData.tile8x = builder.CreateTransientTexture(new TextureDesc(tileClassSizeIntermediateX, tileClassSizeIntermediateY, true, true)
                { colorFormat = GraphicsFormat.R16G16_UInt, enableRandomWrite = true, name = "Material mask 8x" });
                passData.bucketTile8x = builder.CreateTransientTexture(new TextureDesc(tileClassSizeIntermediateX, tileClassSizeIntermediateY, true, true)
                { colorFormat = GraphicsFormat.R8_UInt, enableRandomWrite = true, name = "Bucket mask 8x" });

                passData.tileClassSizeX = tileClassSizeX;
                passData.tileClassSizeY = tileClassSizeY;
                passData.outputTile = builder.WriteTexture(tileClassification);
                passData.outputBucketTile = builder.WriteTexture(bucketID);
                passData.vBufferResources = vBufferResources.Read(builder);

                passData.createMaterialTile = defaultResources.shaders.vbufferTileClassificationCS;

                passData.actualWidth = hdCamera.actualWidth;
                passData.actualHeight = hdCamera.actualHeight;

                builder.SetRenderFunc(
                    (VBufferMaterialTileClassficationData data, RenderGraphContext context) =>
                    {
                        var cs = data.createMaterialTile;
                        var kernel = cs.FindKernel("MaterialReduction");

                        context.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferMaterialTileOutput, data.tile8x);
                        context.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferBucketTileOutput, data.bucketTile8x);

                        int dispatchX = HDUtils.DivRoundUp(data.actualWidth, 8);
                        int dispatchY = HDUtils.DivRoundUp(data.actualHeight, 8);

                        BindVBufferResourcesCS(context.cmd, cs, kernel, data.vBufferResources);

                        context.cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, 1);

                        kernel = cs.FindKernel("MaterialFinalReduction");
                        context.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferMaterialTileInput, data.tile8x);
                        context.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferBucketTileInput, data.bucketTile8x);
                        context.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferMaterialTileOutput, data.outputTile);
                        context.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VisBufferBucketTileOutput, data.outputBucketTile);

                        dispatchX = HDUtils.DivRoundUp(dispatchX, 8);
                        dispatchY = HDUtils.DivRoundUp(dispatchY, 8);

                        context.cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, 1);
                    });
            }
        }

        class VBufferLightingPassData : ForwardOpaquePassData
        {
            public int width;
            public int height;
            public TextureHandle colorBuffer;
            public VBufferInformation vBufferInfo;
            public TextureHandle materialDepthBuffer;
            public TextureHandle cameraDepthTexture;

            //TODO: render lists get deallocated when calling DrawOpaqueRendererList immediately.
            //Workaround is to declare 3 renderer lists for each draw renderes call, which sounds really freaking wasteful.
            //We should instead build the renderer list once, and be able to draw many times. Check with seb.
            //public RendererListHandle rendererList0;
            public RendererListHandle rendererList1;
            public RendererListHandle rendererList2;
        }

        TextureHandle RenderVBufferLighting(
            RenderGraph renderGraph,
            CullingResults cull,
            HDCamera hdCamera,
            VBufferInformation vBufferInfo,
            ShadowResult shadowResult,
            TextureHandle colorBuffer,
            TextureHandle depthBuffer,
            in LightingBuffers lightingBuffers,
            in BuildGPULightListOutput lightLists)
        {
            if (!vBufferInfo.valid)
                return colorBuffer;

            using (var builder = renderGraph.AddRenderPass<VBufferLightingPassData>("VBuffer Lighting", out var passData, ProfilingSampler.Get(HDProfileId.VBufferLighting)))
            {
                var renderListDesc = CreateOpaqueRendererListDesc(
                                    cull,
                                    hdCamera.camera,
                                    HDShaderPassNames.s_VBufferLightingName, m_CurrentRendererConfigurationBakedLighting);
                //TODO: hide this from the UI!!
                renderListDesc.renderingLayerMask = DeferredMaterialBRG.RenderLayerMask;

                PrepareCommonForwardPassData(renderGraph, builder, passData, true, hdCamera.frameSettings, renderListDesc, lightLists, shadowResult);
                passData.rendererList1 = builder.UseRendererList(renderGraph.CreateRendererList(renderListDesc));
                passData.rendererList2 = builder.UseRendererList(renderGraph.CreateRendererList(renderListDesc));

                builder.AllowRendererListCulling(false);

                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.vBufferInfo = vBufferInfo.Read(builder);
                passData.materialDepthBuffer = builder.UseDepthBuffer(passData.vBufferInfo.vBufferResources.vbufferMaterialDepth, DepthAccess.ReadWrite);
                passData.cameraDepthTexture = builder.ReadTexture(depthBuffer);

                passData.enableDecals = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);
                passData.lightingBuffers = ReadLightingBuffers(lightingBuffers, builder);

                builder.SetRenderFunc(
                    (VBufferLightingPassData data, RenderGraphContext context) =>
                    {
                        BindGlobalLightListBuffers(data, context);
                        BindDBufferGlobalData(data.dbuffer, context);
                        BindGlobalLightingBuffers(data.lightingBuffers, context.cmd);

                        context.cmd.SetGlobalTexture(HDShaderIDs._VisBufferDepthTexture, data.cameraDepthTexture);
                        BindVBufferResourcesGlobal(context.cmd, passData.vBufferInfo);

                        int quadTileSize = DeferredMaterialBRG.MaterialTileSize;
                        int numTileX = HDUtils.DivRoundUp(data.width, quadTileSize);
                        int numTileY = HDUtils.DivRoundUp(data.height, quadTileSize);

                        // Note: SHADOWS_SHADOWMASK keyword is enabled in HDRenderPipeline.cs ConfigureForShadowMask
                        bool useFptl = data.frameSettings.IsEnabled(FrameSettingsField.FPTLForForwardOpaque);
                        // say that we want to use tile/cluster light loop
                        CoreUtils.SetKeyword(context.cmd, "USE_FPTL_LIGHTLIST", useFptl);
                        CoreUtils.SetKeyword(context.cmd, "USE_CLUSTERED_LIGHTLIST", !useFptl);

                        context.cmd.SetGlobalVector(HDShaderIDs._VisBufferTileData, new Vector4((float)numTileX, (float)numTileY, (float)quadTileSize, (float)(numTileX * numTileY)));

                        context.cmd.SetViewport(new Rect(0, 0, numTileX * quadTileSize, numTileY * quadTileSize));

                        CoreUtils.SetKeyword(context.cmd, "VARIANT_DIR_ENV", false);
                        CoreUtils.SetKeyword(context.cmd, "VARIANT_DIR_PUNCTUAL_ENV", false);
                        CoreUtils.SetKeyword(context.cmd, "VARIANT_DIR_PUNCTUAL_AREA_ENV", true);
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);

                        CoreUtils.SetKeyword(context.cmd, "VARIANT_DIR_ENV", true);
                        CoreUtils.SetKeyword(context.cmd, "VARIANT_DIR_PUNCTUAL_ENV", false);
                        CoreUtils.SetKeyword(context.cmd, "VARIANT_DIR_PUNCTUAL_AREA_ENV", false);
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList1);

                        CoreUtils.SetKeyword(context.cmd, "VARIANT_DIR_ENV", false);
                        CoreUtils.SetKeyword(context.cmd, "VARIANT_DIR_PUNCTUAL_AREA_ENV", false);
                        CoreUtils.SetKeyword(context.cmd, "VARIANT_DIR_PUNCTUAL_ENV", true);
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList2);
                    });

                PushFullScreenDebugTexture(renderGraph, colorBuffer, FullScreenDebugMode.VisibilityBufferLighting);
            }
            return colorBuffer;
        }
    }
}
