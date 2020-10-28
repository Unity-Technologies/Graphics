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

    internal struct BatchMesh
    {
        internal int hashCode;
        internal int meshCount;
        internal int startHash;
        internal int endHash;
    }

    internal static class Light2DBatch
    {
        static Dictionary<BatchMesh, Mesh> s_BatchMeshes = new Dictionary<BatchMesh,Mesh>();

        static List<CombineInstance> s_ActiveBatch = new List<CombineInstance>();

        static BatchMesh s_ActiveBatchHash = new BatchMesh();

        static List<Mesh> s_FreeMeshes = new List<Mesh>();

        static HashSet<BatchMesh> s_ActiveBatchHashes = new HashSet<BatchMesh>();

        static Material s_ActiveMaterial = null;

        public static Material sActiveMaterial => s_ActiveMaterial;

        internal static void StartScope()
        {
            s_ActiveBatchHashes.Clear();
        }

        internal static void StartBatch(Material mat)
        {
            s_ActiveBatchHash.hashCode = 0;
            s_ActiveBatchHash.meshCount = 0;
            s_ActiveBatchHash.startHash = 0;
            s_ActiveBatchHash.endHash = 0;
            s_ActiveMaterial = mat;
            s_ActiveBatch.Clear();
        }

        internal static void AddMesh(Mesh mesh, Transform transform, int hashCode)
        {
            CombineInstance ci = new CombineInstance();
            ci.mesh = mesh;
            ci.transform = transform.localToWorldMatrix;
            s_ActiveBatch.Add(ci);

            if (s_ActiveBatchHash.startHash == 0)
                s_ActiveBatchHash.startHash = hashCode;
            s_ActiveBatchHash.endHash = hashCode;
            s_ActiveBatchHash.meshCount = s_ActiveBatchHash.meshCount + 1;
            s_ActiveBatchHash.hashCode = s_ActiveBatchHash.hashCode * 16777619 ^ hashCode;
        }

        internal static Mesh EndBatch(ref bool isBatched, ref Matrix4x4 matrix,  ref Material material)
        {
            material = s_ActiveMaterial;
            s_ActiveMaterial = null;
            if (s_ActiveBatch.Count == 1)
            {
                isBatched = false;
                matrix = s_ActiveBatch[0].transform;
                return s_ActiveBatch[0].mesh;
            }

            isBatched = true;
            Mesh mesh = null;
            if (!s_BatchMeshes.TryGetValue(s_ActiveBatchHash, out mesh))
            {
                if (s_FreeMeshes.Count > 0)
                {
                    mesh = s_FreeMeshes[s_FreeMeshes.Count - 1];
                    s_FreeMeshes.RemoveAt(s_FreeMeshes.Count - 1);
                }

                if (mesh == null)
                {
                    mesh = new Mesh();
                    mesh.CombineMeshes(s_ActiveBatch.ToArray());
                    s_BatchMeshes.Add(s_ActiveBatchHash, mesh);
                }
            }
            matrix = Matrix4x4.identity;
            s_ActiveBatchHashes.Add(s_ActiveBatchHash);
            return mesh;
        }

        internal static void EndScope()
        {
            foreach (var batchMesh in s_BatchMeshes)
            {
                if (!s_ActiveBatchHashes.Contains(batchMesh.Key))
                {
                    s_FreeMeshes.Add(batchMesh.Value);
                }
            }
        }
    }

}

