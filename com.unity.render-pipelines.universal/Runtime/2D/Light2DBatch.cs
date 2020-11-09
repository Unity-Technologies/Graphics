using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering.Universal.LibTessDotNet;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace UnityEngine.Experimental.Rendering.Universal
{

    internal struct ShapeMeshBatch
    {
        internal int hashCode;
        internal int meshCount;
        internal int startHash;
        internal int endHash;
    }

    internal static class Light2DBatch
    {

        static Dictionary<ShapeMeshBatch, Mesh> s_BatchMeshes = new Dictionary<ShapeMeshBatch,Mesh>();

        static List<CombineInstance> s_ActiveBatchMeshInstances = new List<CombineInstance>();

        static HashSet<ShapeMeshBatch> s_ActiveBatchHashes = new HashSet<ShapeMeshBatch>();

        static List<Mesh> s_MeshPool = new List<Mesh>();

        static ShapeMeshBatch s_ActiveShapeMeshBatch = new ShapeMeshBatch();

        static Material s_ActiveMaterial = null;

        internal static Material sActiveMaterial => s_ActiveMaterial;

        internal static void StartScope()
        {
            s_ActiveBatchHashes.Clear();
        }

        internal static void StartBatch(Material mat)
        {
            s_ActiveShapeMeshBatch.hashCode = 16777619;
            s_ActiveShapeMeshBatch.meshCount = 0;
            s_ActiveShapeMeshBatch.startHash = 0;
            s_ActiveShapeMeshBatch.endHash = 0;
            s_ActiveMaterial = mat;
            s_ActiveBatchMeshInstances.Clear();
        }

        internal static void AddMesh(Mesh mesh, Transform transform, int hashCode)
        {
            CombineInstance ci = new CombineInstance();
            ci.mesh = mesh;
            ci.transform = transform.localToWorldMatrix;
            s_ActiveBatchMeshInstances.Add(ci);

            if (s_ActiveShapeMeshBatch.startHash == 0)
                s_ActiveShapeMeshBatch.startHash = hashCode;
            s_ActiveShapeMeshBatch.endHash = hashCode;
            s_ActiveShapeMeshBatch.meshCount = s_ActiveShapeMeshBatch.meshCount + 1;
            s_ActiveShapeMeshBatch.hashCode = s_ActiveShapeMeshBatch.hashCode * 16777619 ^ hashCode;
        }

        internal static void EndBatch(CommandBuffer cmd)
        {
            var material = s_ActiveMaterial;
            s_ActiveMaterial = null;
            if (s_ActiveBatchMeshInstances.Count == 1)
            {
                cmd.DrawMesh(s_ActiveBatchMeshInstances[0].mesh, s_ActiveBatchMeshInstances[0].transform, material);
                return;
            }
            
            Mesh mesh = null;
            if (!s_BatchMeshes.TryGetValue(s_ActiveShapeMeshBatch, out mesh))
            {
                if (s_MeshPool.Count > 0)
                {
                    mesh = s_MeshPool[s_MeshPool.Count - 1];
                    mesh.Clear();
                    s_MeshPool.RemoveAt(s_MeshPool.Count - 1);
                }

                if (mesh == null)
                {
                    mesh = new Mesh();
                }

                mesh.CombineMeshes(s_ActiveBatchMeshInstances.ToArray());
                s_BatchMeshes.Add(s_ActiveShapeMeshBatch, mesh);
            }
            
            s_ActiveBatchHashes.Add(s_ActiveShapeMeshBatch);
            cmd.DrawMesh(mesh, Matrix4x4.identity, material);
        }

        internal static void EndScope()
        {
            List<ShapeMeshBatch> unusedMeshes = new List<ShapeMeshBatch>();
            foreach (var batchMesh in s_BatchMeshes)
            {
                if (!s_ActiveBatchHashes.Contains(batchMesh.Key))
                {
                    s_MeshPool.Add(batchMesh.Value);
                    unusedMeshes.Add(batchMesh.Key);
                }
            }
            foreach (var unusedMesh in unusedMeshes)
            {
                s_BatchMeshes.Remove(unusedMesh);
            }
        }
    }

}

