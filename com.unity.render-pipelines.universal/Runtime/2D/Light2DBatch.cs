using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.Universal
{

    internal static class Light2DBatch
    {
        private static readonly int kMaxBatchLimit = 1024;
        
        static Dictionary<int, Mesh> s_BatchMeshes = new Dictionary<int,Mesh>();

        static CombineInstance[] s_ActiveBatchMeshInstances = new CombineInstance[kMaxBatchLimit];

        private static int[] s_UnusedMeshIndices = new int[kMaxBatchLimit];
        
        static HashSet<int> s_ActiveBatchHashes = new HashSet<int>();

        static List<Mesh> s_MeshPool = new List<Mesh>();

        static int s_ActiveShapeMeshBatch = 0;

        static Material s_ActiveMaterial = null;
        
        internal static int s_ActiveMeshes = 0;
        
        internal static int s_Batches = 0; // For tests.
        
        internal static int s_CombineOperations = 0; // For tests.

        static void StartScope()
        {
            s_Batches = 0;
            s_ActiveBatchHashes.Clear();
        }

        static void EndScope()
        {
            int i = 0;
            foreach (var batchMesh in s_BatchMeshes)
            {
                if (!s_ActiveBatchHashes.Contains(batchMesh.Key))
                {
                    s_MeshPool.Add(batchMesh.Value);
                    s_UnusedMeshIndices[i++] = batchMesh.Key;
                }
            }
            for (int j = 0; j < i; ++j)
            {
                s_BatchMeshes.Remove(s_UnusedMeshIndices[j]);
            }
        }

        static void StartBatch(Material mat)
        {
            s_ActiveShapeMeshBatch = 16777619;
            s_ActiveMaterial = mat;
            s_ActiveMeshes = 0;
        }

        static void AddMesh(Mesh mesh, Transform transform, int hashCode)
        {
            CombineInstance ci = new CombineInstance();
            ci.mesh = mesh;
            ci.transform = transform.localToWorldMatrix;
            s_ActiveBatchMeshInstances[s_ActiveMeshes++] = ci;
            
            s_ActiveShapeMeshBatch = s_ActiveShapeMeshBatch * 16777619 ^ hashCode;
            s_ActiveShapeMeshBatch = s_ActiveShapeMeshBatch * 16777619 ^ mesh.GetHashCode();
            s_ActiveShapeMeshBatch = s_ActiveShapeMeshBatch * 16777619 ^ transform.localToWorldMatrix.GetHashCode();
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
                cmd.DrawMesh(s_ActiveBatchMeshInstances[0].mesh, s_ActiveBatchMeshInstances[0].transform, material);
                return;
            }

            Mesh mesh = null;
            if (!s_BatchMeshes.TryGetValue(s_ActiveShapeMeshBatch, out mesh))
            {
                if (s_MeshPool.Count > 0)
                {
                    mesh = s_MeshPool[s_MeshPool.Count - 1];
                    if (mesh) mesh.Clear();
                    s_MeshPool.RemoveAt(s_MeshPool.Count - 1);
                }
                mesh ??= new Mesh();
                
                CombineInstance[] ci = new CombineInstance[s_ActiveMeshes];
                Array.Copy(s_ActiveBatchMeshInstances, ci, s_ActiveMeshes);
                mesh.CombineMeshes(ci);
                s_BatchMeshes.Add(s_ActiveShapeMeshBatch, mesh);
                s_CombineOperations++;
            }

            s_ActiveBatchHashes.Add(s_ActiveShapeMeshBatch);
            cmd.DrawMesh(mesh, Matrix4x4.identity, material);
            s_Batches++;
        }

        internal static void Reset()
        {
            EndScope();
            StartScope();
        }

        internal static bool Batch(CommandBuffer cmd, Light2D light, Material material)
        {
            if (light.lightType == Light2D.LightType.Parametric || light.lightType == Light2D.LightType.Freeform)
            {
                if (!light.shadowsEnabled)
                {
                    if (s_ActiveMaterial == null || s_ActiveMaterial != material || s_ActiveMeshes >= kMaxBatchLimit)
                    {
                        // If this is not the same material, end any previous valid batch.
                        if (s_ActiveMaterial)
                            EndBatch(cmd);
                        StartBatch(material);
                    }

                    AddMesh(light.lightMesh, light.transform, light.hashCode);
                    return true;
                }
            }

            return false;
        }

    }

}

