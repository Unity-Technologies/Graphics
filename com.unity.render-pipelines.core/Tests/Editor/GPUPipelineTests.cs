using NUnit.Framework;
using Unity.Mathematics;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Rendering.Tests
{
    class GPUPipelineTests
    {
        [SetUp]
        public void OnSetup()
        {
        }

        [TearDown]
        public void OnTearDown()
        {
        }

        internal static class TestSchema
        {
            public static readonly int _InternalId0 = Shader.PropertyToID("_InternalId0");
            public static readonly int _BaseColor = Shader.PropertyToID("_BaseColor");
            public static readonly int _TestMatrix = Shader.PropertyToID("_TestMatrix");
            public static readonly int _InternalId1 = Shader.PropertyToID("_InternalId1");
            public static readonly int _TestMatrixInv = Shader.PropertyToID("_TestMatrixInv");
            public static readonly int _InternalId2 = Shader.PropertyToID("_InternalId2");
            public static readonly int _TestMatrix2 = Shader.PropertyToID("_TestMatrix2");
            public static readonly int _VecOffset = Shader.PropertyToID("_VecOffset");
        }

        GPUInstanceDataBuffer ConstructTestInstanceBuffer(int instanceCount)
        {
            using (var builder = new GPUInstanceDataBufferBuilder())
            {
                builder.AddComponent<Vector4>(TestSchema._InternalId0, isOverriden: false, isPerInstance: false);
                builder.AddComponent<Vector4>(TestSchema._BaseColor, isOverriden: true, isPerInstance: true);
                builder.AddComponent<BRGMatrix>(TestSchema._TestMatrix, isOverriden: true, isPerInstance: true);
                builder.AddComponent<BRGMatrix>(TestSchema._TestMatrixInv, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(TestSchema._InternalId1, isOverriden: true, isPerInstance: false);
                builder.AddComponent<Vector4>(TestSchema._InternalId2, isOverriden: true, isPerInstance: false);
                builder.AddComponent<BRGMatrix>(TestSchema._TestMatrix2, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(TestSchema._VecOffset, isOverriden: true, isPerInstance: true);
                return builder.Build(instanceCount);
            }
        }

        struct TestObjectProperties
        {
            public int baseColorIndex;
            public int matrixIndex;
            public int matrixInvIndex;

            public void Initialize(in GPUInstanceDataBuffer buffer)
            {
                baseColorIndex = buffer.GetPropertyIndex(TestSchema._BaseColor);
                matrixIndex = buffer.GetPropertyIndex(TestSchema._TestMatrix);
                matrixInvIndex = buffer.GetPropertyIndex(TestSchema._TestMatrixInv);
            }
        }

        struct TestObject
        {
            public Vector4 color;
            public BRGMatrix matrix;
            public BRGMatrix matrixInv;

            public void Upload(in TestObjectProperties props, ref GPUInstanceDataBufferUploader uploader, int instanceId)
            {
                var instanceHandle = uploader.AllocateInstance(instanceId);
                uploader.WriteParameter<Vector4>(instanceHandle, props.baseColorIndex, color);
                uploader.WriteParameter<BRGMatrix>(instanceHandle, props.matrixIndex, matrix);
                uploader.WriteParameter<BRGMatrix>(instanceHandle, props.matrixInvIndex, matrixInv);
            }

            public void Download(in BigBufferCPUReadbackData readbackData, int insanceId)
            {
                color = readbackData.LoadData<Vector4>(insanceId, TestSchema._BaseColor);
                matrix = readbackData.LoadData<BRGMatrix>(insanceId, TestSchema._TestMatrix);
                matrixInv = readbackData.LoadData<BRGMatrix>(insanceId, TestSchema._TestMatrixInv);
            }

            public bool Equals(in TestObject other)
            {
                return color.Equals(other.color) && matrix.Equals(other.matrix) && matrixInv.Equals(other.matrixInv);
            }

        };

        [Test]
        public void TestBigInstanceBuffer()
        {
            var gpuResources = new GPUInstanceDataBufferUploader.GPUResources();
            using (var instanceBuffer = ConstructTestInstanceBuffer(12))
            {
                var matrixA = new BRGMatrix()
                {
                    localToWorld0 = new float4(1.0f, 2.0f, 3.0f, 4.0f),
                    localToWorld1 = new float4(5.0f, 6.0f, 7.0f, 8.0f),
                    localToWorld2 = new float4(7.0f, 6.0f, 5.0f, 4.0f),
                };
                var matrixB = new BRGMatrix()
                {
                    localToWorld0 = new float4(4.0f, 5.0f, 6.0f, 7.0f),
                    localToWorld1 = new float4(1.0f, 2.0f, 2.0f, 1.0f),
                    localToWorld2 = new float4(0.0f, 1.0f, 1.0f, 0.0f),
                };
                var colorA = new float4(1.0f, 2.0f, 3.0f, 4.0f);
                var colorB = new float4(4.0f, 5.0f, 6.0f, 7.0f);

                var objectA = new TestObject()
                {
                    color = colorA,
                    matrix = matrixA,
                    matrixInv = matrixB
                };

                var objectB = new TestObject()
                {
                    color = colorB,
                    matrix = matrixB,
                    matrixInv = matrixA
                };

                var properties = new TestObjectProperties();
                properties.Initialize(instanceBuffer);

                var instanceUploader0 = new GPUInstanceDataBufferUploader(instanceBuffer);
                {
                    objectA.Upload(properties, ref instanceUploader0, 0);
                    objectA.Upload(properties, ref instanceUploader0, 1);
                    objectA.Upload(properties, ref instanceUploader0, 3);
                    objectA.Upload(properties, ref instanceUploader0, 4);

                    objectB.Upload(properties, ref instanceUploader0, 2);
                    objectB.Upload(properties, ref instanceUploader0, 5);
                    objectB.Upload(properties, ref instanceUploader0, 8);
                    objectB.Upload(properties, ref instanceUploader0, 11);

                    instanceUploader0.SubmitToGpu(ref gpuResources);
                }
                instanceUploader0.Dispose();

                using (var readbackData = new BigBufferCPUReadbackData())
                {
                    readbackData.Load(instanceBuffer);
                    var obj = new TestObject();

                    obj.Download(readbackData, 0);
                    Assert.IsTrue(obj.Equals(objectA));

                    obj.Download(readbackData, 1);
                    Assert.IsTrue(obj.Equals(objectA));

                    obj.Download(readbackData, 3);
                    Assert.IsTrue(obj.Equals(objectA));

                    obj.Download(readbackData, 4);
                    Assert.IsTrue(obj.Equals(objectA));

                    obj.Download(readbackData, 2);
                    Assert.IsTrue(obj.Equals(objectB));

                    obj.Download(readbackData, 5);
                    Assert.IsTrue(obj.Equals(objectB));

                    obj.Download(readbackData, 8);
                    Assert.IsTrue(obj.Equals(objectB));

                    obj.Download(readbackData, 11);
                    Assert.IsTrue(obj.Equals(objectB));

                }
            }
            gpuResources.Dispose();
        }

        [Test]
        public void TestInstancePool()
        {
            var instancePool = new GPUVisibilityInstancePool();
            instancePool.Initialize(5);

            var o = new GameObject();
            var t = o.transform;
            var a = instancePool.AllocateVisibilityEntity(t, true);
            var b = instancePool.AllocateVisibilityEntity(t, true);
            var c = instancePool.AllocateVisibilityEntity(t, true);

            Assert.IsTrue(instancePool.InternalSanityCheckStates());

            instancePool.FreeVisibilityEntity(b);

            Assert.IsTrue(instancePool.InternalSanityCheckStates());

            b = instancePool.AllocateVisibilityEntity(t, true);
            var d = instancePool.AllocateVisibilityEntity(t, true);
            var e = instancePool.AllocateVisibilityEntity(t, true);

            Assert.IsTrue(instancePool.InternalSanityCheckStates());

            instancePool.FreeVisibilityEntity(b);
            instancePool.FreeVisibilityEntity(e);
            instancePool.FreeVisibilityEntity(a);

            Assert.IsTrue(instancePool.InternalSanityCheckStates());

            var g = instancePool.AllocateVisibilityEntity(t, true);

            Assert.IsTrue(instancePool.InternalSanityCheckStates());


            instancePool.Dispose();
        }

        //Validates that the visible instance passed exist in the culling results
        void ValidateInstancesAreVisible(
            NativeList<GPUVisibilityInstance> expectedVisibleInstances,
            NativeList<GeometryPoolHandle> expectedGeoPoolHandles,
            GeometryPool geometryPool,
            GPUInstanceDataBuffer bigInstanceBuffer,
            in ClusterCullingResultsCPUReadbackData cullingResults)
        {
            //download geometry pool && the big buffer results to the CPU
            using (var geoPoolResults = new GeometryPoolCPUReadbackData())
            {
                geoPoolResults.Load(geometryPool);
                using (var bigBufferResults = new BigBufferCPUReadbackData())
                {
                    Assert.IsTrue(cullingResults.drawArgs.instanceCount == 1, "Should always be 1 instance for this index buffer");
                    Assert.IsTrue(cullingResults.drawArgs.indexCountPerInstance > 0, "Should have more than at least 1 index.");
                    bigBufferResults.Load(bigInstanceBuffer);

                    var instanceToVisibleClusterCounts = new Dictionary<int, int>();
                    Assert.IsTrue((cullingResults.visibleIndexData.Length % 3) == 0, "Number of indices must be multiple of 3");
                    for (int i = 0; i < cullingResults.visibleIndexData.Length; i += 3)
                    {
                        int packedData0 = cullingResults.visibleIndexData[i + 0];
                        int packedData1 = cullingResults.visibleIndexData[i + 1];
                        int packedData2 = cullingResults.visibleIndexData[i + 2];
                        int clusterOffset0 = packedData0 >> 8;
                        int clusterOffset1 = packedData1 >> 8;
                        int clusterOffset2 = packedData2 >> 8;
                        Assert.IsTrue(clusterOffset0 == clusterOffset1);
                        Assert.IsTrue(clusterOffset1 == clusterOffset2);

                        var clusterPackedData = cullingResults.visibleClusterData[clusterOffset0];
                        int visibleInstanceID = clusterPackedData & 0xFFFF;
                        int visibleClusterIndex = clusterPackedData >> 16;

                        if (!instanceToVisibleClusterCounts.TryGetValue(visibleInstanceID, out var currCounts))
                            instanceToVisibleClusterCounts.Add(visibleInstanceID, 1);
                        else
                            instanceToVisibleClusterCounts[visibleInstanceID] = currCounts + 1;
                    }

                    for (int i = 0; i < expectedVisibleInstances.Length; ++i)
                    {
                        GPUVisibilityInstance visibleInstanceID = expectedVisibleInstances[i];
                        Assert.IsTrue(instanceToVisibleClusterCounts[visibleInstanceID.index] > 0); //must be at least 1 cluster visible.

                        float sampledGeometryHandleFloat = bigBufferResults.LoadData<Vector4>(visibleInstanceID.index, GPUInstanceDataBuffer.DefaultSchema._DeferredMaterialInstanceData).x;
                        int sampledGeometryHandle;
                        unsafe { sampledGeometryHandle = *((int*)&sampledGeometryHandleFloat); };
                        Assert.IsTrue(expectedGeoPoolHandles[i].index == sampledGeometryHandle, "Mismatch of geometry pool handle from instance.");

                        GeoPoolMeshEntry meshEntry = geoPoolResults.gpuMeshEntries[sampledGeometryHandle];

                        Assert.IsTrue(meshEntry.clustersCounts == instanceToVisibleClusterCounts[visibleInstanceID.index] / GeometryPoolConstants.GeoPoolClusterPrimitiveCount, "The testing function expects full visibility on cluster counts.");
                    }

                    Assert.IsTrue(instanceToVisibleClusterCounts.Count == expectedVisibleInstances.Length, "Unexpected number of visible instances. The number of visible instances passed must match exactly the visible instances given by the culler. ");
                }
            }
        }

        [Test]
        public void TestInstanceClusterVisibility()
        {
            var go0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            var objList = new List<MeshRenderer>();
            objList.Add(go0.GetComponent<UnityEngine.MeshRenderer>());
            objList.Add(go1.GetComponent<UnityEngine.MeshRenderer>());
            objList.Add(go2.GetComponent<UnityEngine.MeshRenderer>());

            using (var brg = new GPUVisibilityBRG())
            {
                CommandBuffer cmdBuffer = new CommandBuffer();
                brg.Initialize(GPUVisibilityBRGDesc.NewDefault());
                var batchID = brg.CreateBatchFromGameObjectInstances(objList);
                brg.Update();

                //Run transform jobs twice, only should perform cmdBuffer once since we update only transforms that change.
                int commandsWritten = 0;
                brg.StartInstanceTransformUpdateJobs();
                if (brg.EndInstanceTransformUpdateJobs(cmdBuffer))
                    ++commandsWritten;

                brg.StartInstanceTransformUpdateJobs();
                if (brg.EndInstanceTransformUpdateJobs(cmdBuffer))
                    ++commandsWritten;

                Assert.IsTrue(commandsWritten == 1, "should only run once, since transforms are dirty at the begining");

                //Reset counter
                commandsWritten = 0;
                if (brg.RunCulling(cmdBuffer))
                    ++commandsWritten;

                Assert.IsTrue(commandsWritten == 1, "should have at least 1 instance registered, thus running must run once.");

                Graphics.ExecuteCommandBuffer(cmdBuffer);

                //download GPU culling results to the  CPU
                using (var cullingResults = new ClusterCullingResultsCPUReadbackData())
                {
                    cullingResults.Load(brg.drawVisibleIndicesArgsBuffer, brg.visibleIndexBuffer, brg.visibleClustersBuffer);

                    GPUInstanceBatchData batchData = brg.GetBatchData(batchID);
                    ValidateInstancesAreVisible(batchData.instances, batchData.geoPoolHandles, brg.geometryPool, brg.bigInstanceBuffer, cullingResults);
                }

                brg.DestroyInstanceBatch(batchID);
                cmdBuffer.Release();
            }
        }
    }
}
