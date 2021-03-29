using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace UnityEngine.Rendering.MeshDecal
{
    public partial class MeshDecalProjector
    {
        const float MaxLocalZNormal = -0.1f;

        static List<Mesh> sourceMeshBuffer;

        void GenerateDecalMesh()
        {
            // var stopwatch = new System.Diagnostics.Stopwatch();
            // stopwatch.Start();

            // Collect source meshes and matrices
            if (sourceMeshBuffer == null)
                sourceMeshBuffer = new List<Mesh>();

            var matrixList = new NativeList<float4x4>(Allocator.TempJob);
            var toLocalMatrix = transform.worldToLocalMatrix;

            for (var i = 0; i < meshFilters.Count; i++)
            {
                var filter = meshFilters[i];
                if (filter == null || filter == meshFilter || filter.TryGetComponent<MeshDecalProjector>(out _))
                    continue;
                var sourceMesh = filter.sharedMesh;
                if (sourceMesh == null)
                    continue;

#if !UNITY_EDITOR
            if (!sourceMesh.isReadable)
                continue;
#endif

                sourceMeshBuffer.Add(sourceMesh);
                matrixList.Add(toLocalMatrix * filter.transform.localToWorldMatrix);
            }

#if UNITY_EDITOR
            var meshDataArray = MeshUtility.AcquireReadOnlyMeshData(sourceMeshBuffer);
#else
        var meshDataArray = Mesh.AcquireReadOnlyMeshData(sourceMeshBuffer);
#endif
            sourceMeshBuffer.Clear();

            // Generate mesh
            var decalMeshDataArray = Mesh.AllocateWritableMeshData(1);

            GenerateMeshData(meshDataArray, matrixList, m_Size, offset, new float4(m_uvRect.x, m_uvRect.y, m_uvRect.width, m_uvRect.height), normalBlend, decalMeshDataArray[0]);

            meshDataArray.Dispose();
            matrixList.Dispose();

            // Apply to mesh
            Mesh.ApplyAndDisposeWritableMeshData(decalMeshDataArray, mesh);
            mesh.RecalculateBounds();

            // stopwatch.Stop();
            // Debug.Log($"Decal mesh generated in {stopwatch.Elapsed.TotalMilliseconds} ms.");
        }

        /// <summary>
        /// Combines meshes transformed by specified matrices, cuts triangles off with a box size and fill output MeshData.
        /// </summary>
        static void GenerateMeshData(Mesh.MeshDataArray sourceMeshes, NativeList<float4x4> matrices,
            float3 boxSize, float backOffset, float4 uvRect, float normalBlend, Mesh.MeshData output)
        {
            new GenerateMeshDataJob
            {
                sourceMeshes = sourceMeshes,
                matrices = matrices,
                boxSize = boxSize,
                backOffset = backOffset,
                normalBlend = math.saturate(normalBlend),
                uvRect = uvRect,
                output = output
            }.Run();
        }

        /// <summary>
        /// A job that builds a decal mesh and writes it to the specified MeshData.
        /// </summary>
        [BurstCompile]
        struct GenerateMeshDataJob : IJob
        {
            [ReadOnly] public Mesh.MeshDataArray sourceMeshes;
            [ReadOnly] public NativeArray<float4x4> matrices;
            public float3 boxSize;
            public float backOffset;
            public float normalBlend;
            public float4 uvRect;

            public Mesh.MeshData output;

            public void Execute()
            {
                var meshBuffers = new MeshBuffers(192, Allocator.Temp);

                GenerateTriangles(sourceMeshes, matrices, boxSize, backOffset, normalBlend, meshBuffers);

                var descriptors = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp)
                {
                    [0] = new VertexAttributeDescriptor(VertexAttribute.Position),
                    [1] = new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1),
                    [2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, stream: 2, dimension: 4),
                    [3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, stream: 3, dimension: 2)
                };
                output.SetVertexBufferParams(meshBuffers.vertices.Length, descriptors);
                descriptors.Dispose();

                GenerateSecondaryMeshData(meshBuffers, boxSize, uvRect, output);

                meshBuffers.Dispose();
            }
        }

        /// <summary>
        /// Vertex and index buffers to fill while building a mesh.
        /// </summary>
        struct MeshBuffers : IDisposable
        {
            public NativeList<float3> vertices;
            public NativeList<float3> normals;
            public NativeList<int> indices;

            public MeshBuffers(int initialCapacity, Allocator allocator)
            {
                vertices = new NativeList<float3>(initialCapacity, allocator);
                normals = new NativeList<float3>(initialCapacity, allocator);
                indices = new NativeList<int>(initialCapacity, allocator);
            }

            public void Clear()
            {
                vertices.Clear();
                normals.Clear();
                indices.Clear();
            }

            public void Dispose()
            {
                vertices.Dispose();
                normals.Dispose();
                indices.Dispose();
            }
        }

        /// <summary>
        /// Fills mesh buffers with combined and cut source meshes. Burst-friendly.
        /// </summary>
        static void GenerateTriangles(Mesh.MeshDataArray sourceMeshes, NativeArray<float4x4> matrices,
            float3 boxSize, float backOffset, float normalBlend, MeshBuffers output)
        {
            var boxExtents = boxSize * 0.5f;

            var polygonBuffers = new MeshBuffers(3, Allocator.Temp);
            var cuttingBuffer = new NativeList<float>(3, Allocator.Temp);

            for (var i = 0; i < sourceMeshes.Length; i++)
            {
                var matrix = matrices[i];

                var sourceMesh = sourceMeshes[i];

                if (!sourceMesh.HasVertexAttribute(VertexAttribute.Position))
                    continue;

                // Skip meshes without normals for now, but it would be better to calculate something for them.
                if (!sourceMesh.HasVertexAttribute(VertexAttribute.Normal))
                    continue;

                // Copy and transform all vertices and normals, apply offset
                var sourceVertices = new NativeArray<float3>(sourceMesh.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                sourceMesh.GetVertices(sourceVertices.Reinterpret<Vector3>());
                for (var j = 0; j < sourceVertices.Length; j++)
                    sourceVertices[j] = math.mul(matrix, new float4(sourceVertices[j], 1)).xyz;

                var sourceNormals = new NativeArray<float3>(sourceMesh.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                sourceMesh.GetNormals(sourceNormals.Reinterpret<Vector3>());
                for (var j = 0; j < sourceNormals.Length; j++)
                    sourceNormals[j] = math.normalize(math.mul(matrix, new float4(sourceNormals[j], 0)).xyz);

                // Copy all indices for each submesh and process all triangles
                for (var j = 0; j < sourceMesh.subMeshCount; j++)
                {
                    var submesh = sourceMesh.GetSubMesh(j);
                    if (sourceMesh.indexFormat == IndexFormat.UInt32)
                    {
                        var sourceIndices = new NativeArray<int>(submesh.indexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        sourceMesh.GetIndices(sourceIndices, j);
                        for (var k = 0; k < sourceIndices.Length; k += 3)
                        {
                            ProcessSourceTriangle(sourceVertices, sourceNormals, boxExtents,
                                sourceIndices[k], sourceIndices[k + 1], sourceIndices[k + 2],
                                polygonBuffers, cuttingBuffer, output);
                        }
                    }
                    else
                    {
                        var sourceIndices = new NativeArray<ushort>(submesh.indexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        sourceMesh.GetIndices(sourceIndices, j);
                        for (var k = 0; k < sourceIndices.Length; k += 3)
                        {
                            ProcessSourceTriangle(sourceVertices, sourceNormals, boxExtents,
                                sourceIndices[k], sourceIndices[k + 1], sourceIndices[k + 2],
                                polygonBuffers, cuttingBuffer, output);
                        }
                    }
                }

                sourceVertices.Dispose();
                sourceNormals.Dispose();
            }

            // Apply offset
            if (backOffset != 0f)
            {
                for (var i = 0; i < output.vertices.Length; i++)
                {
                    var vertex = output.vertices[i];
                    vertex.z -= backOffset;
                    output.vertices[i] = vertex;
                }
            }

            // Apply normal blend or just normalize
            if (normalBlend != 1f)
            {
                for (var i = 0; i < output.normals.Length; i++)
                    output.normals[i] = math.normalize(math.lerp(new float3(0, 0, -1), output.normals[i], normalBlend));
            }
            else
            {
                for (var i = 0; i < output.normals.Length; i++)
                    output.normals[i] = math.normalize(output.normals[i]);
            }

            polygonBuffers.Dispose();
        }

        /// <summary>
        /// Fills polygon buffers, calls its processing and clears them after.
        /// </summary>
        static void ProcessSourceTriangle(NativeArray<float3> sourceVertices, NativeArray<float3> sourceNormals, float3 boxExtents,
            int index0, int index1, int index2, MeshBuffers polygonBuffers, NativeList<float> cuttingBuffer, MeshBuffers output)
        {
            var position0 = sourceVertices[index0];
            var position1 = sourceVertices[index1];
            var position2 = sourceVertices[index2];

            // Discard back faces
            // if (float3.Cross(position2 - position0, position1 - position0).z <= 0)
            //     return;

            // Fill polygon buffers with triangle data
            polygonBuffers.vertices.Add(position0);
            polygonBuffers.vertices.Add(position1);
            polygonBuffers.vertices.Add(position2);
            polygonBuffers.normals.Add(sourceNormals[index0]);
            polygonBuffers.normals.Add(sourceNormals[index1]);
            polygonBuffers.normals.Add(sourceNormals[index2]);

            ProcessPolygon(polygonBuffers, cuttingBuffer, boxExtents, output);

            polygonBuffers.Clear();
        }

        /// <summary>
        /// Cuts off outer parts of the polygon and triangulates it into output buffers.
        /// </summary>
        static void ProcessPolygon(MeshBuffers polygon, NativeList<float> cuttingBuffer, float3 boxExtents, MeshBuffers output)
        {
            // Cut outer parts of the polygon
            CutOffPolygon(polygon.vertices, polygon.normals, cuttingBuffer, new float4(-1, 0, 0, boxExtents.x));
            if (polygon.vertices.Length < 3)
                return;
            CutOffPolygon(polygon.vertices, polygon.normals, cuttingBuffer, new float4(1, 0, 0, boxExtents.x));
            if (polygon.vertices.Length < 3)
                return;
            CutOffPolygon(polygon.vertices, polygon.normals, cuttingBuffer, new float4(0, -1, 0, boxExtents.y));
            if (polygon.vertices.Length < 3)
                return;
            CutOffPolygon(polygon.vertices, polygon.normals, cuttingBuffer, new float4(0, 1, 0, boxExtents.y));
            if (polygon.vertices.Length < 3)
                return;
            CutOffPolygon(polygon.vertices, polygon.normals, cuttingBuffer, new float4(0, 0, -1, boxExtents.z));
            if (polygon.vertices.Length < 3)
                return;
            CutOffPolygon(polygon.vertices, polygon.normals, cuttingBuffer, new float4(0, 0, 1, boxExtents.z));
            if (polygon.vertices.Length < 3)
                return;

            // Cut back-facing parts of the polygon (same as positional cut but in "normal space", with buffers swapped)
            CutOffPolygon(polygon.normals, polygon.vertices, cuttingBuffer, new float4(0, 0, 1, MaxLocalZNormal));
            if (polygon.vertices.Length < 3)
                return;

            // Add vertices
            for (var i = 0; i < polygon.vertices.Length; i++)
                polygon.indices.Add(AddVertex(output.vertices, output.normals, polygon.vertices[i], polygon.normals[i]));

            // Triangulate non-triangular polygon
            while (polygon.vertices.Length > 3)
            {
                // Find shortest cutting edge
                var bestPrev = polygon.vertices.Length - 1;
                var bestIndex = 0;
                var bestNext = 1;
                var bestDistance = math.distancesq(polygon.vertices[1], polygon.vertices[polygon.vertices.Length - 1]);
                for (var j = 1; j < polygon.vertices.Length; j++)
                {
                    var prevIndex = j - 1;
                    var nextIndex = j + 1;
                    if (nextIndex == polygon.vertices.Length)
                        nextIndex = 0;
                    var newDistance = math.distancesq(polygon.vertices[prevIndex], polygon.vertices[nextIndex]);
                    if (newDistance < bestDistance)
                    {
                        bestDistance = newDistance;
                        bestPrev = prevIndex;
                        bestIndex = j;
                        bestNext = nextIndex;
                    }
                }

                // Add outer triangle
                output.indices.Add(polygon.indices[bestPrev]);
                output.indices.Add(polygon.indices[bestIndex]);
                output.indices.Add(polygon.indices[bestNext]);

                // Remove it from the polygon (we won't use normals anymore, so don't bother removing)
                polygon.vertices.RemoveAt(bestIndex);
                polygon.indices.RemoveAt(bestIndex);
            }

            // Add last triangle of the polygon
            output.indices.Add(polygon.indices[0]);
            output.indices.Add(polygon.indices[1]);
            output.indices.Add(polygon.indices[2]);
        }

        /// <summary>
        /// Cuts off parts of a polygon by a plane.
        /// </summary>
        static void CutOffPolygon(NativeList<float3> primaryValues, NativeList<float3> secondaryValues, NativeList<float> cuttingBuffer, float4 plane)
        {
            // Calculate distances from the plane to primary values (can be either position or normal)
            for (var i = 0; i < primaryValues.Length; i++)
                cuttingBuffer.Add(math.dot(plane.xyz, primaryValues[i]) - plane.w);

            // Split intersecting edges by adding interpolated vertices
            for (var i = 0; i < cuttingBuffer.Length; i++)
            {
                var distance = cuttingBuffer[i];
                var nextIndex = i + 1;
                if (nextIndex == cuttingBuffer.Length)
                    nextIndex = 0;
                var nextDistance = cuttingBuffer[nextIndex];

                // Check edge intersection
                if (distance >= 0f && nextDistance >= 0f || distance <= 0f && nextDistance <= 0f)
                    continue;

                // Add vertex
                distance = math.abs(distance);
                nextDistance = math.abs(nextDistance);
                var length = distance + nextDistance;

                var newVertexPrimaryValue = (primaryValues[i] * nextDistance + primaryValues[nextIndex] * distance) / length;
                var newVertexSecondaryValue = (secondaryValues[i] * nextDistance + secondaryValues[nextIndex] * distance) / length;
                nextIndex = i + 1;
                NativeListInsert(primaryValues, nextIndex, newVertexPrimaryValue);
                NativeListInsert(secondaryValues, nextIndex, newVertexSecondaryValue);
                NativeListInsert(cuttingBuffer, nextIndex, 0f);

                i++;
            }

            // Remove outside vertices
            for (var i = cuttingBuffer.Length - 1; i >= 0; i--)
            {
                if (cuttingBuffer[i] > 0f)
                {
                    primaryValues.RemoveAt(i);
                    secondaryValues.RemoveAt(i);
                }
            }

            cuttingBuffer.Clear();
        }

        /// <summary>
        /// Insert an element in NativeList at the specified index.
        /// </summary>
        static void NativeListInsert<T>(NativeList<T> list, int index, T element) where T : struct
        {
            var length = list.Length;
            list.ResizeUninitialized(length + 1);
            for (var i = length; i > index; i--)
                list[i] = list[i - 1];
            list[index] = element;
        }

        /// <summary>
        /// Adds a vertex to output buffers and returns its index.
        /// </summary>
        static int AddVertex(NativeList<float3> outputVertices, NativeList<float3> outputNormals, float3 position, float3 normal)
        {
            var index = outputVertices.Length;

            // Obviously, this search is extremely slow. Need to find another way to reduce vertex splitting
            /* for (var i = 0; i < index; i++)
            {
                if (math.all(outputVertices[i] == position & outputNormals[i] == normal))
                    return i;
            } */

            outputVertices.Add(position);
            outputNormals.Add(normal);
            return index;
        }

        /// <summary>
        /// Generates UVs, tangents and writes vertices and indices into mesh data.
        /// </summary>
        static void GenerateSecondaryMeshData(MeshBuffers buffers, float3 boxSize, float4 uvRect, Mesh.MeshData outputMesh)
        {
            outputMesh.GetVertexData<float3>().CopyFrom(buffers.vertices);

            var normals = outputMesh.GetVertexData<float3>(1);
            normals.CopyFrom(buffers.normals);

            var vertexCount = buffers.vertices.Length;

            // Generate UVs
            var uvs = outputMesh.GetVertexData<float2>(3);
            for (var i = 0; i < vertexCount; i++)
            {
                var uv = buffers.vertices[i].xy;
                uv = uv / boxSize.xy + 0.5f;
                uv = (uv * uvRect.zw) + uvRect.xy;
                uvs[i] = uv;
            }

            // Generate tangents
            var tangents = outputMesh.GetVertexData<float4>(2);
            for (var i = 0; i < vertexCount; i++)
                tangents[i] = new float4(math.normalize(math.cross(normals[i], new float3(0f, 1f, 0f))), -1f);

            // Generate index data
            var indexCount = buffers.indices.Length;
            if (indexCount > 65535)
            {
                outputMesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
                outputMesh.GetIndexData<int>().CopyFrom(buffers.indices);
            }
            else
            {
                outputMesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
                var newIndices = outputMesh.GetIndexData<ushort>();
                for (var i = 0; i < indexCount; i++)
                    newIndices[i] = (ushort)buffers.indices[i];
            }

            // Set sub mesh
            outputMesh.subMeshCount = 1;
            outputMesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount));
        }
    }
}
