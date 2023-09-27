using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Tests
{
    internal struct MeshTestData : IDisposable
    {
        public Mesh cube;
        public Mesh sphere;
        public Mesh capsule;
        public Mesh cube16bit;
        public Mesh capsule16bit;
        public Mesh mergedCubeSphere;
        public Mesh mergedSphereCube;

        public void Initialize()
        {
            // SetupGeometryPoolTests
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshFilter>().sharedMesh;
            capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule).GetComponent<MeshFilter>().sharedMesh;
            cube16bit = Create16BitIndexMesh(cube);
            capsule16bit = Create16BitIndexMesh(capsule);

            var newCube = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>();
            mergedCubeSphere = MergeMeshes(cube, sphere);
            mergedSphereCube = MergeMeshes(sphere, cube);
        }

        public static Mesh Create16BitIndexMesh(Mesh input)
        {
            Mesh newMesh = new Mesh();
            newMesh.vertices = new Vector3[input.vertexCount];
            System.Array.Copy(input.vertices, newMesh.vertices, input.vertexCount);

            newMesh.uv = new Vector2[input.vertexCount];
            System.Array.Copy(input.uv, newMesh.uv, input.vertexCount);

            newMesh.normals = new Vector3[input.vertexCount];
            System.Array.Copy(input.normals, newMesh.normals, input.vertexCount);

            newMesh.vertexBufferTarget = GraphicsBuffer.Target.Raw;
            newMesh.indexBufferTarget = GraphicsBuffer.Target.Raw;

            newMesh.subMeshCount = input.subMeshCount;

            int indexCounts = 0;
            for (int i = 0; i < input.subMeshCount; ++i)
                indexCounts += (int)input.GetIndexCount(i);

            newMesh.SetIndexBufferParams(indexCounts, IndexFormat.UInt16);

            for (int i = 0; i < input.subMeshCount; ++i)
            {
                newMesh.SetSubMesh(i, input.GetSubMesh(i), MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
                newMesh.SetIndices(input.GetIndices(i), MeshTopology.Triangles, i);
            }

            newMesh.UploadMeshData(false);
            return newMesh;
        }

        public static Mesh MergeMeshes(Mesh a, Mesh b)
        {
            Mesh newMesh = new Mesh();
            CombineInstance[] c = new CombineInstance[2];
            var ca = new CombineInstance();
            ca.transform = Matrix4x4.identity;
            ca.subMeshIndex = 0;
            ca.mesh = a;

            var cb = new CombineInstance();
            cb.transform = Matrix4x4.identity;
            cb.subMeshIndex = 0;
            cb.mesh = b;
            c[0] = ca;
            c[1] = cb;

            newMesh.CombineMeshes(c, false);
            newMesh.UploadMeshData(false);
            return newMesh;
        }

        public void Dispose()
        {
            cube = null;
            sphere = null;
            capsule = null;
            cube16bit = null;
            capsule16bit = null;
            mergedCubeSphere = null;
            mergedSphereCube = null;
        }
    }

    //Helper class containing a snapshot of the GPU big instance buffer data
    internal struct InstanceDataBufferCPUReadbackData : IDisposable
    {
        public NativeArray<Vector4> data;
        GPUInstanceDataBuffer m_InstanceDataBuffer;

        public void Load(GPUInstanceDataBuffer instanceDataBuffer)
        {
            m_InstanceDataBuffer = instanceDataBuffer;
            var cmdBuffer = new CommandBuffer();
            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            var localData = new NativeArray<Vector4>(instanceDataBuffer.byteSize / vec4Size, Allocator.Persistent);
            cmdBuffer.RequestAsyncReadback(instanceDataBuffer.gpuBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    localData.CopyFrom(req.GetData<Vector4>());
            });
            cmdBuffer.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmdBuffer);
            cmdBuffer.Release();
            data = localData;
        }

        public T LoadData<T>(int instanceId, int propertyID) where T : unmanaged
        {
            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            int propertyIndex = m_InstanceDataBuffer.GetPropertyIndex(propertyID);
            Assert.IsTrue(m_InstanceDataBuffer.descriptions[propertyIndex].isPerInstance);
            int gpuBaseAddress = m_InstanceDataBuffer.gpuBufferComponentAddress[propertyIndex];
            int indexInArray = (gpuBaseAddress + m_InstanceDataBuffer.descriptions[propertyIndex].byteSize * instanceId) / vec4Size;

            unsafe
            {
                Vector4* dataPtr = (Vector4*)data.GetUnsafePtr<Vector4>() + indexInArray;
                T result = *(T*)(dataPtr);
                return result;
            }
        }

        public void Dispose()
        {
            data.Dispose();
        }
    }

    internal class RenderPassTest : RenderPipelineAsset
    {
        public delegate void TestDelegate(ScriptableRenderContext ctx, Camera[] cameras);

        protected override RenderPipeline CreatePipeline()
        {
            return new RenderPassTestCullInstance(this);
        }
    }

    internal class RenderPassTestCullInstance : RenderPipeline
    {
        RenderPassTest m_Owner;
        public RenderPassTestCullInstance(RenderPassTest owner)
        {
            m_Owner = owner;
        }

        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            foreach (var camera in cameras)
            {
                if (!camera.enabled)
                    continue;

                ScriptableCullingParameters cullingParams;
                camera.TryGetCullingParameters(out cullingParams);
                renderContext.Cull(ref cullingParams);
            }
            renderContext.Submit();
        }
    }
}
