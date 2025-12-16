using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Tests
{
    class RenderGraphViewerDebugDataSerializationTests
    {
        static RenderGraph.DebugData CreateTestDebugData()
        {
            RenderGraph.DebugData debugData = new("TestExecution");

            var resourceReadLists = new RenderGraph.DebugData.PassData.ResourceIdLists();
            var resourceWriteLists = new RenderGraph.DebugData.PassData.ResourceIdLists();

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            {
                resourceReadLists[i] = new List<int> { 2, 4, 6 };
                resourceWriteLists[i] = new List<int> { 3, 5, 7 };
            }

            var nativePassAttachment = new NativePassAttachment(
                new ResourceHandle(),
                RenderBufferLoadAction.Clear,
                RenderBufferStoreAction.StoreAndResolve,
                true,
                2,
                1
            );

            var nativeRenderPassInfo = new RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo
            {
                passBreakReasoning = "Example Break Reason",
                attachmentInfos = new List<RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo.AttachmentInfo>
                {
                    new()
                    {
                        resourceName = "TestResource",
                        loadReason = "Load Test",
                        storeReason = "Store Test",
                        storeMsaaReason = "Store MSAA Test",
                        attachmentIndex = 1,
                        attachment = new RenderGraph.DebugData.SerializableNativePassAttachment(nativePassAttachment)
                    }
                },
                mergedPassIds = new List<int> { 1, 2, 3 }
            };

            nativeRenderPassInfo.passCompatibility = new();
            nativeRenderPassInfo.passCompatibility.Add(0, new RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo.PassCompatibilityInfo { message = "Compatible", isCompatible = true });

            var nrpInfo = new RenderGraph.DebugData.PassData.NRPInfo
            {
                nativePassInfo = nativeRenderPassInfo,
                textureFBFetchList = new List<int> { 10, 20, 30 },
                setGlobals = new List<int> { 5, 6, 7 },
                width = 1920,
                height = 1080,
                volumeDepth = 1,
                samples = 4,
                hasDepth = true
            };

            var passData = new RenderGraph.DebugData.PassData
            {
                name = "RenderPass1",
                type = RenderGraphPassType.Unsafe,
                resourceReadLists = resourceReadLists,
                resourceWriteLists = resourceWriteLists,
                culled = false,
                async = true,
                nativeSubPassIndex = 2,
                syncToPassIndex = -1,
                syncFromPassIndex = -1,
                generateDebugData = true
            };

            passData.nrpInfo = nrpInfo;
            debugData.passList.Add(passData);

            return debugData;
        }

        [Test]
        public void DebugData_IsSerializedCorrectly()
        {
            var debugData = CreateTestDebugData();
            var passData = debugData.passList[0];
            var json = RenderGraph.DebugDataSerialization.ToJson(debugData);

            var jsonDebugData = RenderGraph.DebugDataSerialization.FromJson(json);
            var jsonPassData = jsonDebugData.passList[0];

            Assert.AreEqual(passData.name, jsonPassData.name);
            Assert.AreEqual(passData.type, jsonPassData.type);

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            {
                CollectionAssert.AreEqual(passData.resourceReadLists[i], jsonPassData.resourceReadLists[i]);
                CollectionAssert.AreEqual(passData.resourceWriteLists[i], jsonPassData.resourceWriteLists[i]);
            }

            Assert.AreEqual(passData.culled, jsonPassData.culled);
            Assert.AreEqual(passData.async, jsonPassData.async);
            Assert.AreEqual(passData.nativeSubPassIndex, jsonPassData.nativeSubPassIndex);
            Assert.AreEqual(passData.syncToPassIndex, jsonPassData.syncToPassIndex);
            Assert.AreEqual(passData.syncFromPassIndex, jsonPassData.syncFromPassIndex);
            Assert.AreEqual(passData.generateDebugData, jsonPassData.generateDebugData);

            Assert.NotNull(jsonPassData.nrpInfo);
            Assert.NotNull(jsonPassData.nrpInfo.nativePassInfo);

            var nativePassInfo = passData.nrpInfo.nativePassInfo;
            var jsonNativePassInfo = jsonPassData.nrpInfo.nativePassInfo;

            Assert.AreEqual(nativePassInfo.passBreakReasoning, jsonNativePassInfo.passBreakReasoning);
            Assert.AreEqual(nativePassInfo.attachmentInfos.Count, jsonNativePassInfo.attachmentInfos.Count);
            Assert.NotNull(jsonNativePassInfo.mergedPassIds);
            Assert.AreEqual(nativePassInfo.mergedPassIds.Count, jsonNativePassInfo.mergedPassIds.Count);
            Assert.AreEqual(nativePassInfo.mergedPassIds, jsonNativePassInfo.mergedPassIds);
            Assert.NotNull(jsonNativePassInfo.passCompatibility);
            Assert.AreEqual(nativePassInfo.passCompatibility.Count, jsonNativePassInfo.passCompatibility.Count);
            Assert.AreEqual(nativePassInfo.passCompatibility[0].isCompatible, jsonNativePassInfo.passCompatibility[0].isCompatible);
            Assert.AreEqual(nativePassInfo.passCompatibility[0].message, jsonNativePassInfo.passCompatibility[0].message);

            var attInfo = passData.nrpInfo.nativePassInfo.attachmentInfos[0];
            var jsonAttInfo = jsonPassData.nrpInfo.nativePassInfo.attachmentInfos[0];

            Assert.AreEqual(attInfo.attachmentIndex, jsonAttInfo.attachmentIndex);
            Assert.AreEqual(attInfo.resourceName, jsonAttInfo.resourceName);
            Assert.AreEqual(attInfo.attachment.depthSlice, jsonAttInfo.attachment.depthSlice);
            Assert.AreEqual(attInfo.attachment.mipLevel, jsonAttInfo.attachment.mipLevel);
            Assert.AreEqual(attInfo.attachment.loadAction, jsonAttInfo.attachment.loadAction);
            Assert.AreEqual(attInfo.attachment.storeAction, jsonAttInfo.attachment.storeAction);

            Assert.AreEqual(passData.nrpInfo.width, jsonPassData.nrpInfo.width);
            Assert.AreEqual(passData.nrpInfo.height, jsonPassData.nrpInfo.height);
            Assert.AreEqual(passData.nrpInfo.volumeDepth, jsonPassData.nrpInfo.volumeDepth);
            Assert.AreEqual(passData.nrpInfo.samples, jsonPassData.nrpInfo.samples);
            Assert.AreEqual(passData.nrpInfo.hasDepth, jsonPassData.nrpInfo.hasDepth);
            Assert.AreEqual(passData.nrpInfo.textureFBFetchList, jsonPassData.nrpInfo.textureFBFetchList);
            Assert.AreEqual(passData.nrpInfo.setGlobals, jsonPassData.nrpInfo.setGlobals);
        }

        [Test]
        public void DebugMessageHandler_DebugDataMessage_IsSerializedCorrectly()
        {
            var payload = new DebugMessageHandler.DebugDataPayload()
            {
                version = DebugMessageHandler.k_Version,
                graphName = "TestGraph",
#pragma warning disable 618 // todo @emilie.thaulow replace with unique id
                executionId = 123,
#pragma warning restore 618
                debugData = CreateTestDebugData()
            };

            var bytes = DebugMessageHandler.SerializeMessage(DebugMessageHandler.MessageType.DebugData, payload);
            var (deserializedMessageType, deserializedPayload) = DebugMessageHandler.DeserializeMessage(bytes);

            var deserializedDebugDataPayload = deserializedPayload as DebugMessageHandler.DebugDataPayload;
            Assert.NotNull(deserializedDebugDataPayload);
            Assert.AreEqual(DebugMessageHandler.MessageType.DebugData, deserializedMessageType);
            Assert.AreEqual(payload.version, deserializedDebugDataPayload.version);
            Assert.AreEqual(payload.executionId, deserializedDebugDataPayload.executionId);
            Assert.AreEqual(payload.graphName, deserializedDebugDataPayload.graphName);
            Assert.NotNull(deserializedDebugDataPayload.debugData);
        }

        const string k_TestDataPath = "Tests/Editor/RenderGraphViewer/SerializedDebugDataMessage.data";

        // If this test fails, it means that something in RenderGraphViewer DebugMessageHandler and/or DebugData have changed
        // in a way that is incompatible. This means you must increment DebugMessageHandler.k_Version. In addition, you need
        // to update SerializedDebugDataMessage.data to ensure the next time the data changes, this test fails. Use the
        // Tests > Rendering > Generate RenderGraphViewer\SerializedDebugDataMessage.data menuitem to regenerate the file
        // after bumping the version number.
        [Test]
        public void DebugMessageHandler_DebugDataMessage_IsCompatibleWithSerializedVersion()
        {
            var path = $"{Application.dataPath}/{k_TestDataPath}";
            Assume.That(File.Exists(path), $"Test data file not found: {path}");

            var bytes = File.ReadAllBytes(path);
            var (deserializedMessageType, deserializedPayload) = DebugMessageHandler.DeserializeMessage(bytes);

            var deserializedDebugDataPayload = deserializedPayload as DebugMessageHandler.DebugDataPayload;
            Assert.NotNull(deserializedDebugDataPayload);
            Assert.AreEqual(DebugMessageHandler.MessageType.DebugData, deserializedMessageType);
            Assert.True(deserializedPayload.isCompatible);
            Assert.AreEqual(DebugMessageHandler.k_Version, deserializedPayload.version);

#pragma warning disable 618 // todo @emilie.thaulow replace with unique id
            Assert.AreEqual(123, (int)deserializedDebugDataPayload.executionId);
#pragma warning restore 618
            Assert.AreEqual("TestGraph", deserializedDebugDataPayload.graphName);
            Assert.NotNull(deserializedDebugDataPayload.debugData);
        }

        [MenuItem("Tests/Rendering/Generate RenderGraphViewer\\SerializedDebugDataMessage.data")]
        public static void DoSomething()
        {
            var payload = new DebugMessageHandler.DebugDataPayload()
            {
                version = DebugMessageHandler.k_Version,
                graphName = "TestGraph",
#pragma warning disable 618 // todo @emilie.thaulow replace with unique id
                executionId = 123,
#pragma warning restore 618
                debugData = CreateTestDebugData()
            };

            var bytes = DebugMessageHandler.SerializeMessage(DebugMessageHandler.MessageType.DebugData, payload);
            var path = $"{Application.dataPath}/{k_TestDataPath}";
            File.WriteAllBytes(path, bytes);
        }

        [Test]
        public void DebugMessageHandler_DebugDataMessage_OldVersionPrintsWarning()
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);
            writer.Write((byte)DebugMessageHandler.MessageType.DebugData);
            writer.Write(0);
            LogAssert.Expect(LogType.Warning, $"Render Graph Viewer message version mismatch (expected {DebugMessageHandler.k_Version}, received 0)");
            var (_, payload) = DebugMessageHandler.DeserializeMessage(memoryStream.ToArray());
            Assert.False(payload.isCompatible);
        }

        [Test]
        public void DebugMessageHandler_DebugDataMessage_InvalidDataThrows()
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);
            writer.Write((byte)DebugMessageHandler.MessageType.DebugData);
            writer.Write(DebugMessageHandler.k_Version);
            // Missing stuff that the deserializer expects
            Assert.Throws<EndOfStreamException>(() => DebugMessageHandler.DeserializeMessage(memoryStream.ToArray()));
        }

        [Test]
        public void DebugMessageHandler_AnalyticsDataMessage_IsSerializedCorrectly()
        {
            var payload = new DebugMessageHandler.AnalyticsPayload()
            {
                version = DebugMessageHandler.k_Version,
                deviceModel = "device model",
                deviceType = DeviceType.Desktop,
                graphicsDeviceType = GraphicsDeviceType.Direct3D11,
                gpuVendor = "gpu vendor",
                gpuName = "gpu name"
            };

            var bytes = DebugMessageHandler.SerializeMessage(DebugMessageHandler.MessageType.AnalyticsData, payload);
            var (deserializedMessageType, deserializedPayload) = DebugMessageHandler.DeserializeMessage(bytes);

            var deserializedAnalyticsPayload = deserializedPayload as DebugMessageHandler.AnalyticsPayload;
            Assert.NotNull(deserializedAnalyticsPayload);
            Assert.AreEqual(DebugMessageHandler.MessageType.AnalyticsData, deserializedMessageType);
            Assert.AreEqual(payload.version, deserializedAnalyticsPayload.version);
            Assert.AreEqual(payload.deviceModel, deserializedAnalyticsPayload.deviceModel);
            Assert.AreEqual(payload.deviceType, deserializedAnalyticsPayload.deviceType);
            Assert.AreEqual(payload.graphicsDeviceType, deserializedAnalyticsPayload.graphicsDeviceType);
            Assert.AreEqual(payload.gpuVendor, deserializedAnalyticsPayload.gpuVendor);
            Assert.AreEqual(payload.gpuName, deserializedAnalyticsPayload.gpuName);
        }

        [Test]
        public void DebugMessageHandler_AnalyticsDataMessage_OldVersionIsIncompatible()
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);
            writer.Write((byte)DebugMessageHandler.MessageType.AnalyticsData);
            writer.Write(0);
            var (_, payload) = DebugMessageHandler.DeserializeMessage(memoryStream.ToArray());
            Assert.False(payload.isCompatible);
        }
    }
}
