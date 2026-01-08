using System;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Tests
{
    internal struct GPUDrivenTestHelper
    {
        static public uint4 UnpackUintTo4x8Bit(uint val)
        {
            return new uint4(val & 0xFF, (val >> 8) & 0xFF, (val >> 16) & 0xFF, (val >> 24) & 0xFF);
        }
    }

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
            cube.name = "Cube";
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshFilter>().sharedMesh;
            capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule).GetComponent<MeshFilter>().sharedMesh;
            cube16bit = Create16BitIndexMesh(cube);
            cube16bit.name = "Cube16bit";
            capsule16bit = Create16BitIndexMesh(capsule);
            capsule16bit.name = "Capsule16bit";

            mergedCubeSphere = MergeMeshes(cube, sphere);
            mergedCubeSphere.name = "MergedCubeSphere";
            mergedSphereCube = MergeMeshes(sphere, cube);
            mergedSphereCube.name = "MergedSphereCube";
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

    internal class RenderPassTest : RenderPipelineAsset<RenderPassTestCullInstance>
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

        protected internal override bool IsRenderRequestSupported<RequestData>(Camera camera, RequestData data)
        {
            return true;
        }

        protected override void ProcessRenderRequests<RequestData>(ScriptableRenderContext renderContext, Camera camera, RequestData renderRequest)
        {
            if (!camera.enabled)
                return;

            ScriptableCullingParameters cullingParams;
            camera.TryGetCullingParameters(out cullingParams);
            renderContext.Cull(ref cullingParams);

            renderContext.Submit();
        }

        protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
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
    
    [SupportedOnRenderPipeline(typeof(RenderPassTest))]
    [System.ComponentModel.DisplayName("RenderPass")]
    internal class RenderPassGlobalSettings : RenderPipelineGlobalSettings<RenderPassGlobalSettings, RenderPassTestCullInstance>
    {
        [SerializeField] RenderPipelineGraphicsSettingsContainer m_Settings = new();
        protected override List<IRenderPipelineGraphicsSettings> settingsList => m_Settings.settingsList;
    }
}
