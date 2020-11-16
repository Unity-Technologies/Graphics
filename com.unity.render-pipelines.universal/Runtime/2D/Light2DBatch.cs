using System.Collections.Generic;
using System.Numerics;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.Universal.LibTessDotNet;

namespace UnityEngine.Experimental.Rendering.Universal
{

    struct LightBatchInfo
    {
        
        public Light2D light;
        
        public Matrix4x4 matrix;
    }
    
    internal static class Light2DBatch
    {
        
        static readonly int kMaxBatchLimit = 1024;
        
        static readonly int kGeometryDataLimit = 32000;
        
        static Dictionary<int, Mesh> s_BatchMeshes = new Dictionary<int, Mesh>();

        static LightBatchInfo[] s_ActiveBatchLights = new LightBatchInfo[kMaxBatchLimit];

        static HashSet<int> s_ActiveFrameHashes = new HashSet<int>();

        static List<Mesh> s_MeshPool = new List<Mesh>();

        static int s_ActiveBatchHash = 0;

        static Material s_ActiveMaterial = null;
        
        internal static int s_BatchedVertexCount = 0;

        internal static int s_BatchedIndexCount = 0;
        
        internal static int s_ActiveMeshes = 0;
        
        internal static int s_Batches = 0; // For tests.
        
        internal static int s_CombineOperations = 0; // For tests.

        static void StartBatch(Material mat)
        {
            s_ActiveBatchHash = 16777619;
            s_ActiveMaterial = mat;
            s_ActiveMeshes = 0;
            s_BatchedIndexCount = 0;
            s_BatchedVertexCount = 0;
        }

        static void AddMesh(Light2D light)
        {
            LightBatchInfo ci = new LightBatchInfo();
            ci.light = light;
            ci.matrix = light.transform.localToWorldMatrix;
            s_ActiveBatchLights[s_ActiveMeshes++] = ci;
            s_BatchedVertexCount += light.lightMesh.vertexCount;
            s_BatchedIndexCount += (int)light.lightMesh.GetIndexCount(0); 
            
            s_ActiveBatchHash = s_ActiveBatchHash * 16777619 ^ light.hashCode;
            s_ActiveBatchHash = s_ActiveBatchHash * 16777619 ^ light.lightMesh.GetHashCode();
            s_ActiveBatchHash = s_ActiveBatchHash * 16777619 ^ ci.matrix.GetHashCode();
        }

        internal static void EndBatch(CommandBuffer cmd)
        {
            if (null == s_ActiveMaterial)
                return;

            var material = s_ActiveMaterial;
            s_ActiveMaterial = null;
            if (s_ActiveMeshes == 1)
            {
                // If there is only one in batch, just call current one.
                cmd.DrawMesh(s_ActiveBatchLights[0].light.lightMesh, s_ActiveBatchLights[0].matrix, material);
                return;
            }

            Mesh mesh = null;
            if (!s_BatchMeshes.TryGetValue(s_ActiveBatchHash, out mesh))
            {
                if (s_MeshPool.Count > 0)
                {
                    mesh = s_MeshPool[s_MeshPool.Count - 1];
                    if (mesh) mesh.Clear();
                    s_MeshPool.RemoveAt(s_MeshPool.Count - 1);
                }
                if (!mesh)
                    mesh = new Mesh();
                
                Combine(mesh, s_ActiveMeshes);
                s_BatchMeshes.Add(s_ActiveBatchHash, mesh);
                s_CombineOperations++;
            }

            s_ActiveFrameHashes.Add(s_ActiveBatchHash);
            cmd.DrawMesh(mesh, Matrix4x4.identity, material);
            s_Batches++;
        }

        internal static void Combine(Mesh target, int count)
        {
            NativeArray<LightUtility.LightMeshVertex> vertices = new NativeArray<LightUtility.LightMeshVertex>(s_BatchedVertexCount, Allocator.Temp);
            NativeArray<ushort> indices = new NativeArray<ushort>(s_BatchedIndexCount, Allocator.Temp);
            int vertexCount = 0, indexCount = 0;
            for (int i = 0; i < s_ActiveMeshes; ++i)
            {
                for (int j = 0; j < s_ActiveBatchLights[i].light.indices.Length; ++j)
                    indices[indexCount++] = (ushort)(vertexCount + s_ActiveBatchLights[i].light.indices[j]);
                for (int j = 0; j < s_ActiveBatchLights[i].light.vertices.Length; ++j)
                {
                    LightUtility.LightMeshVertex lmv = s_ActiveBatchLights[i].light.vertices[j];
                    lmv.position = s_ActiveBatchLights[i].matrix.MultiplyPoint(lmv.position);
                    vertices[vertexCount++] = lmv;
                }
            }
            target.SetVertexBufferParams(vertexCount, LightUtility.LightMeshVertex.VertexLayout);
            target.SetVertexBufferData(vertices, 0, 0, vertexCount);
            target.SetIndices(indices, 0, indexCount, MeshTopology.Triangles, 0, true);
        }
        
        internal static void Reset()
        {
            int i = 0;
            NativeArray<int> unusedMeshes = new NativeArray<int>(s_BatchMeshes.Count, Allocator.Temp);
            foreach (var batchMesh in s_BatchMeshes)
            {
                if (!s_ActiveFrameHashes.Contains(batchMesh.Key))
                {
                    s_MeshPool.Add(batchMesh.Value);
                    unusedMeshes[i++] = batchMesh.Key;
                }
            }
            for (int j = 0; j < i; ++j)
            {
                s_BatchMeshes.Remove(unusedMeshes[j]);
            }            

            s_Batches = 0;
            s_ActiveFrameHashes.Clear();
        }

        internal static bool Batch(CommandBuffer cmd, Light2D light, Material material)
        {
            if (!light.shadowsEnabled && light.normalMapQuality == Light2D.NormalMapQuality.Disabled && (light.lightType == Light2D.LightType.Parametric || light.lightType == Light2D.LightType.Freeform))
            {
                if (s_ActiveMaterial == null || s_ActiveMaterial != material || s_ActiveMeshes >= kMaxBatchLimit || s_BatchedIndexCount > kGeometryDataLimit || s_BatchedVertexCount > kGeometryDataLimit)
                {
                    // If this is not the same material, end any previous valid batch.
                    if (s_ActiveMaterial)
                        EndBatch(cmd);
                    StartBatch(material);
                }

                AddMesh(light);
                return true;
            }

            return false;
        }

    }

}

