using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;

namespace UnityEditor.Rendering.Tests
{
    internal static class RenderGraphDebugSessionMockFactory
    {
        public const string k_GraphName = "URPRenderGraph";
        public const string k_ExecutionName = "Main Camera";
        public const ulong k_ExecutionIdValue = 1099511677449;

        public static IRenderGraphDebugSession CreateMockedSession()
        {
            var debugData = CreateUrpDebugData();
            var container = new DebugDataContainer();
            container.AddGraph(k_GraphName);
            container.AddExecution(k_GraphName, EntityId.FromULong(k_ExecutionIdValue), k_ExecutionName);
            container.SetDebugData(k_GraphName, EntityId.FromULong(k_ExecutionIdValue), debugData);

            return new MockDebugSession(container);
        }

        private static RenderGraph.DebugData CreateUrpDebugData()
        {
            var debugData = new RenderGraph.DebugData(k_ExecutionName)
            {
                valid = true,
                captureSourceString = "Editor",
                captureTimestamp = "16:56:55",
                graphHash = 1592559333
            };

            // Add all 17 passes
            debugData.passList.Add(CreatePass("Initialize Frame", RenderGraphPassType.Unsafe, 0, 1000));
            debugData.passList.Add(CreatePass("Setup Forward Lights", RenderGraphPassType.Unsafe, 1, 1001));
            debugData.passList.Add(CreatePass("Setup Debug Properties", RenderGraphPassType.Raster, 2, 1002, nativeSubPassIndex: -1, nativePassInfo: CreateNativePassInfo("Failed to merge ProcessVFXCameraCommand into this native pass.\nThe next pass is not a raster render pass.", new[] { 2, 3 })));
            debugData.passList.Add(CreatePass("Setup Camera Properties", RenderGraphPassType.Raster, 3, 1003, nativeSubPassIndex: -1, nativePassInfo: CreateNativePassInfo("", new[] { 2, 3 })));
            debugData.passList.Add(CreatePass("Process VFXCamera Command", RenderGraphPassType.Unsafe, 4, 1004));
            debugData.passList.Add(CreatePass("Draw Main Light Shadowmap", RenderGraphPassType.Raster, 5, 1005, nativeSubPassIndex: -1, setGlobals: new[] { 3 }, nativePassInfo: CreateNativePassInfo("Failed to merge DrawDepthNormalPrepass into this native pass.\nThe render target sizes of the next pass do not match.", new[] { 5, 6 })));
            debugData.passList.Add(CreatePass("Setup Camera Properties", RenderGraphPassType.Raster, 6, 1006, nativeSubPassIndex: -1, nativePassInfo: CreateNativePassInfo("", new[] { 5, 6 })));
            debugData.passList.Add(CreatePassWithResources("Draw Depth Normal Prepass", RenderGraphPassType.Raster, 7, 1007,
                readTextures: new[] { 8 }, writeTextures: new[] { 9, 8 }, nativeSubPassIndex: 0,
                setGlobals: new[] { 9, 8 }, width: 1470, height: 689, hasDepth: true,
                nativePassInfo: CreateNativePassInfoWithAttachments("Failed to merge Copy DBuffer Depth into this native pass.\nThe next pass uses a different depth buffer. All passes in the native render pass need to use the same depth buffer.",
                    new[] { 7 },
                    new[]
                    {
                        ("_CameraDepthTexture", "The resource is created in this pass and cleared on first use.", "The resource is read by pass <b>Draw UIToolkit/uGUI Overlay</b> executed later in the graph. The data is stored.", 0, RenderBufferLoadAction.Clear, RenderBufferStoreAction.Store),
                        ("_CameraNormalsTexture", "The resource is created in this pass and cleared on first use.", "The resource is read by pass <b>Draw UIToolkit/uGUI Overlay</b> executed later in the graph. The data is stored.", 1, RenderBufferLoadAction.Clear, RenderBufferStoreAction.Store)
                    })));
            debugData.passList.Add(CreatePassWithResources("Copy DBuffer Depth", RenderGraphPassType.Raster, 8, 1008,
                readTextures: new[] { 8 }, writeTextures: new[] { 12 }, nativeSubPassIndex: 0, width: 1470, height: 689, hasDepth: true,
                nativePassInfo: CreateNativePassInfoWithAttachments("Failed to merge DrawOpaqueObjects into this native pass.\nThe next pass uses a different depth buffer. All passes in the native render pass need to use the same depth buffer.",
                    new[] { 8, 9 },
                    new[]
                    {
                        ("DBufferDepth", "The pass indicated it will rewrite the full resource contents. Existing contents are not loaded or cleared.", "The resource is written by this pass but no later passes are using the results. The data is discarded.", 0, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare),
                        ("_DBufferTexture0", "The resource is created in this pass and cleared on first use.", "The resource is read by pass <b>Draw UIToolkit/uGUI Overlay</b> executed later in the graph. The data is stored.", 1, RenderBufferLoadAction.Clear, RenderBufferStoreAction.Store),
                        ("_DBufferTexture1", "The resource is created in this pass and cleared on first use.", "The resource is read by pass <b>Draw UIToolkit/uGUI Overlay</b> executed later in the graph. The data is stored.", 2, RenderBufferLoadAction.Clear, RenderBufferStoreAction.Store),
                        ("_DBufferTexture2", "The resource is created in this pass and cleared on first use.", "The resource is read by pass <b>Draw UIToolkit/uGUI Overlay</b> executed later in the graph. The data is stored.", 3, RenderBufferLoadAction.Clear, RenderBufferStoreAction.Store)
                    })));
            debugData.passList.Add(CreatePassWithResources("Draw DBuffer", RenderGraphPassType.Raster, 9, 1009,
                readTextures: new[] { 12, 8, 9 }, writeTextures: new[] { 13, 14, 15 }, nativeSubPassIndex: 1,
                setGlobals: new[] { 13, 14, 15 }, width: 1470, height: 689, hasDepth: true,
                nativePassInfo: CreateNativePassInfo("", new[] { 8, 9 })));
            debugData.passList.Add(CreatePassWithResources("Draw Opaque Objects", RenderGraphPassType.Raster, 10, 1010,
                readTextures: new[] { 7, 3, 13, 14, 15, 3, 9, 8, 13, 14, 15 }, writeTextures: new[] { 6, 7 }, nativeSubPassIndex: 0,
                width: 1470, height: 689, hasDepth: true,
                nativePassInfo: CreateNativePassInfoWithAttachments("Failed to merge BlitFinalToBackBuffer into this native pass.\nThe next pass reads data output by this pass as a regular texture.",
                    new[] { 10, 11, 12, 13 },
                    new[]
                    {
                        ("_CameraDepthAttachment", "The resource is created in this pass and cleared on first use.", "The resource is read by pass <b>Draw UIToolkit/uGUI Overlay</b> executed later in the graph. The data is stored.", 0, RenderBufferLoadAction.Clear, RenderBufferStoreAction.Store),
                        ("_CameraTargetAttachment", "The resource is created in this pass and cleared on first use.", "The resource is read by pass <b>BlitFinalToBackBuffer</b> executed later in the graph. The data is stored.", 1, RenderBufferLoadAction.Clear, RenderBufferStoreAction.Store)
                    })));
            debugData.passList.Add(CreatePassWithResources("Draw Decal Forward Emissive", RenderGraphPassType.Raster, 11, 1011,
                readTextures: new[] { 7 }, writeTextures: new[] { 6 }, nativeSubPassIndex: 1, width: 1470, height: 689, hasDepth: true,
                nativePassInfo: CreateNativePassInfo("", new[] { 10, 11, 12, 13 })));
            debugData.passList.Add(CreatePassWithResources("Draw Skybox", RenderGraphPassType.Raster, 12, 1012,
                readTextures: new[] { 7 }, writeTextures: new[] { 6, 7 }, nativeSubPassIndex: 2, width: 1470, height: 689, hasDepth: true,
                nativePassInfo: CreateNativePassInfo("", new[] { 10, 11, 12, 13 })));
            debugData.passList.Add(CreatePassWithResources("Draw Transparent Objects", RenderGraphPassType.Raster, 13, 1013,
                readTextures: new[] { 7, 3, 13, 14, 15, 3, 9, 8, 13, 14, 15 }, writeTextures: new[] { 6, 7 }, nativeSubPassIndex: 2,
                width: 1470, height: 689, hasDepth: true,
                nativePassInfo: CreateNativePassInfo("", new[] { 10, 11, 12, 13 })));
            debugData.passList.Add(CreatePassWithResources("Blit Final To Back Buffer", RenderGraphPassType.Raster, 14, 1014,
                readTextures: new[] { 6 }, writeTextures: new[] { 4 }, nativeSubPassIndex: 0, width: 1470, height: 689,
                nativePassInfo: CreateNativePassInfoWithAttachments("Failed to merge Draw IMGUI/SoftwareCursor Overlay into this native pass.\nThe next pass is not a raster render pass.",
                    new[] { 14, 15 },
                    new[]
                    {
                        ("_CameraDepthAttachment", "The resource is written by <b>DrawOpaqueObjects</b> executed previously in the graph. The data is loaded.", "The resource is written by this pass but no later passes are using the results. The data is discarded.", 0, RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare),
                        ("Backbuffer color", "The resource is imported in the graph and loaded to retrieve the existing buffer contents.", "The resource is read by pass <b>Draw IMGUI/SoftwareCursor Overlay</b> executed later in the graph. The data is stored.", 1, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store)
                    })));
            debugData.passList.Add(CreatePassWithResources("Draw UIToolkit/u GUI Overlay", RenderGraphPassType.Raster, 15, 1015,
                readTextures: new[] { 7, 3, 9, 8, 13, 14, 15 }, writeTextures: new[] { 4, 7 }, nativeSubPassIndex: 1,
                width: 1470, height: 689, hasDepth: true,
                nativePassInfo: CreateNativePassInfo("", new[] { 14, 15 })));
            debugData.passList.Add(CreatePassWithResources("Draw IMGUI/Software Cursor Overlay", RenderGraphPassType.Unsafe, 16, 1016,
                writeTextures: new[] { 4 }));

            // Add all 16 texture resources
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("<null>", true, -1, -1, 0, 0, 0, 0, GraphicsFormat.None));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("UnityBlack", true, -1, -1, 4, 4, 1, 1, GraphicsFormat.R8G8B8A8_SRGB));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("UnityWhite", true, -1, -1, 4, 4, 1, 1, GraphicsFormat.R8G8B8A8_SRGB));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("DefaultShadowTexture", true, 10, 15, 1, 1, 1, 1, GraphicsFormat.R32_SFloat, new[] { 10, 10, 13, 13, 15 }));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("Backbuffer color", true, 14, 16, 1470, 689, 1, 1, GraphicsFormat.R8G8B8A8_SRGB, producerList: new[] { 14, 15, 16 }));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("Backbuffer depth", true, -1, -1, 1470, 689, 1, 1, GraphicsFormat.D32_SFloat_S8_UInt));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("_CameraTargetAttachment", false, 10, 14, 1470, 689, 1, 1, GraphicsFormat.R8G8B8A8_SRGB, new[] { 14 }, new[] { 10, 11, 12, 13 }));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("_CameraDepthAttachment", false, 10, 15, 1470, 689, 1, 1, GraphicsFormat.D32_SFloat_S8_UInt, new[] { 10, 11, 12, 13, 15 }, new[] { 10, 12, 13, 15 }));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("_CameraDepthTexture", false, 7, 15, 1470, 689, 1, 1, GraphicsFormat.D32_SFloat_S8_UInt, new[] { 7, 8, 9, 10, 13, 15 }, new[] { 7 }));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("_CameraNormalsTexture", false, 7, 15, 1470, 689, 1, 1, GraphicsFormat.A2B10G10R10_UNormPack32, new[] { 9, 10, 13, 15 }, new[] { 7 }));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("_MotionVectorTexture", false, -1, -1, 1470, 689, 1, 1, GraphicsFormat.R16G16_SFloat));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("_MotionVectorDepthTexture", false, -1, -1, 1470, 689, 1, 1, GraphicsFormat.D32_SFloat_S8_UInt));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("DBufferDepth", false, 8, 9, 1470, 689, 1, 1, GraphicsFormat.D32_SFloat_S8_UInt, new[] { 9 }, new[] { 8 }));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("_DBufferTexture0", false, 9, 15, 1470, 689, 1, 1, GraphicsFormat.R8G8B8A8_SRGB, new[] { 10, 10, 13, 13, 15 }, new[] { 9 }));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("_DBufferTexture1", false, 9, 15, 1470, 689, 1, 1, GraphicsFormat.R8G8B8A8_UNorm, new[] { 10, 10, 13, 13, 15 }, new[] { 9 }));
            debugData.resourceLists[(int)RenderGraphResourceType.Texture].Add(CreateTextureResource("_DBufferTexture2", false, 9, 15, 1470, 689, 1, 1, GraphicsFormat.R8G8B8A8_UNorm, new[] { 10, 10, 13, 13, 15 }, new[] { 9 }));

            // Add buffer resource
            debugData.resourceLists[(int)RenderGraphResourceType.Buffer].Add(CreateBufferResource("<null>", true, -1, -1));

            // Add acceleration structure resource
            debugData.resourceLists[(int)RenderGraphResourceType.AccelerationStructure].Add(CreateAccelerationStructureResource("<null>", true, -1, -1));

            return debugData;
        }

        private static RenderGraph.DebugData.PassData CreatePass(string name, RenderGraphPassType type, int passIndex, int nrpRid,
            int nativeSubPassIndex = -1, int[] setGlobals = null, int? reuseNrpInfoId = null,
            RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo nativePassInfo = null)
        {
            var pass = new RenderGraph.DebugData.PassData
            {
                name = name,
                type = type,
                resourceReadLists = new RenderGraph.DebugData.PassData.ResourceIdLists(),
                resourceWriteLists = new RenderGraph.DebugData.PassData.ResourceIdLists(),
                culled = false,
                async = false,
                nativeSubPassIndex = nativeSubPassIndex,
                syncToPassIndex = -1,
                syncFromPassIndex = -1,
                generateDebugData = true,
                nrpInfo = new RenderGraph.DebugData.PassData.NRPInfo
                {
                    nativePassInfo = nativePassInfo,
                    textureFBFetchList = new List<int>(),
                    setGlobals = setGlobals != null ? new List<int>(setGlobals) : new List<int>(),
                    width = 0,
                    height = 0,
                    volumeDepth = 0,
                    samples = 0,
                    hasDepth = false
                },
                scriptInfo = new RenderGraph.DebugData.ScriptInfo
                {
                    filePath = $"C:\\UnitySrc\\unity\\Packages\\com.unity.render-pipelines.universal\\Runtime\\ScriptableRenderer.cs",
                    line = 618
                }
            };

            InitializeResourceLists(pass.resourceReadLists);
            InitializeResourceLists(pass.resourceWriteLists);

            return pass;
        }

        private static RenderGraph.DebugData.PassData CreatePassWithResources(string name, RenderGraphPassType type, int passIndex, int nrpRid,
            int[] readTextures = null, int[] writeTextures = null, int nativeSubPassIndex = -1, int[] setGlobals = null,
            int width = 0, int height = 0, bool hasDepth = false, int? reuseNrpInfoId = null,
            RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo nativePassInfo = null)
        {
            var pass = CreatePass(name, type, passIndex, nrpRid, nativeSubPassIndex, setGlobals, reuseNrpInfoId, nativePassInfo);

            if (readTextures != null)
                pass.resourceReadLists[(int)RenderGraphResourceType.Texture] = new List<int>(readTextures);
            if (writeTextures != null)
                pass.resourceWriteLists[(int)RenderGraphResourceType.Texture] = new List<int>(writeTextures);

            pass.nrpInfo.width = width;
            pass.nrpInfo.height = height;
            pass.nrpInfo.volumeDepth = width > 0 ? 1 : 0;
            pass.nrpInfo.samples = width > 0 ? 1 : 0;
            pass.nrpInfo.hasDepth = hasDepth;

            return pass;
        }

        private static void InitializeResourceLists(RenderGraph.DebugData.PassData.ResourceIdLists lists)
        {
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            {
                lists[i] = new List<int>();
            }
        }

        private static RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo CreateNativePassInfo(string breakReason, int[] mergedPassIds)
        {
            return new RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo
            {
                passBreakReasoning = breakReason,
                attachmentInfos = new (),
                passCompatibility = new (),
                mergedPassIds = new List<int>(mergedPassIds)
            };
        }

        private static RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo CreateNativePassInfoWithAttachments(
            string breakReason, int[] mergedPassIds,
            (string name, string loadReason, string storeReason, int attachmentIndex, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction)[] attachments)
        {
            var info = CreateNativePassInfo(breakReason, mergedPassIds);

            foreach (var att in attachments)
            {
                var nativePassAttachment = new NativePassAttachment(
                    new ResourceHandle(),
                    att.loadAction,
                    att.storeAction,
                    false,
                    0,
                    -1
                );

                info.attachmentInfos.Add(new RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo.AttachmentInfo
                {
                    resourceName = att.name,
                    loadReason = att.loadReason,
                    storeReason = att.storeReason,
                    storeMsaaReason = "",
                    attachmentIndex = att.attachmentIndex,
                    attachment = new RenderGraph.DebugData.SerializableNativePassAttachment(nativePassAttachment)
                });
            }

            return info;
        }

        private static RenderGraph.DebugData.ResourceData CreateTextureResource(string name, bool imported,
            int creationPass, int releasePass, int width, int height, int depth, int samples, GraphicsFormat format,
            int[] consumerList = null, int[] producerList = null)
        {
            return new RenderGraph.DebugData.ResourceData
            {
                name = name,
                imported = imported,
                creationPassIndex = creationPass,
                releasePassIndex = releasePass,
                consumerList = consumerList != null ? new List<int>(consumerList) : new List<int>(),
                producerList = producerList != null ? new List<int>(producerList) : new List<int>(),
                memoryless = false,
                textureData = new RenderGraph.DebugData.TextureResourceData
                {
                    width = width,
                    height = height,
                    depth = depth,
                    bindMS = false,
                    samples = samples,
                    format = format,
                    clearBuffer = !imported && creationPass >= 0
                },
                bufferData = null
            };
        }

        private static RenderGraph.DebugData.ResourceData CreateBufferResource(string name, bool imported, int creationPass, int releasePass)
        {
            return new RenderGraph.DebugData.ResourceData
            {
                name = name,
                imported = imported,
                creationPassIndex = creationPass,
                releasePassIndex = releasePass,
                consumerList = new List<int>(),
                producerList = new List<int>(),
                memoryless = false,
                textureData = null,
                bufferData = new RenderGraph.DebugData.BufferResourceData()
            };
        }

        private static RenderGraph.DebugData.ResourceData CreateAccelerationStructureResource(string name, bool imported, int creationPass, int releasePass)
        {
            return CreateBufferResource(name, imported, creationPass, releasePass);
        }

        private class MockDebugSession : IRenderGraphDebugSession
        {
            readonly DebugDataContainer m_Container;

            public MockDebugSession(DebugDataContainer container)
            {
                m_Container = container;
            }

            public event Action onRegisteredGraphsChanged { add { } remove { } }
            public event Action<string, EntityId> onDebugDataUpdated { add { } remove { } }
            public event Action<bool> onPausedStateChanged { add { } remove { } }

            public bool isActive => true;
            public bool isPaused { get; set; } = true;
            public string connectionName { get; set; } = string.Empty;
            public DebugDataContainer debugDataContainer => m_Container;

            public List<string> GetRegisteredGraphs() => m_Container.GetRenderGraphs();
            public List<RenderGraph.DebugExecutionItem> GetExecutions(string graphName) => m_Container.GetExecutions(graphName);
            public RenderGraph.DebugData GetDebugData(string graphName, EntityId executionId) => m_Container.GetDebugData(graphName, executionId);
            public void SetDebugData(string renderGraph, EntityId executionId, RenderGraph.DebugData data) { }
            public void DeleteExecutionIds(string renderGraph, List<EntityId> executionIds) { }
            public void Dispose() { }
        }
    }
}
