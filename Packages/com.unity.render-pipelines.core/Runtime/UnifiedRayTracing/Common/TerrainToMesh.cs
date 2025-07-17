using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal static class TerrainToMesh
    {
        static private AsyncTerrainToMeshRequest MakeAsyncTerrainToMeshRequest(int width, int height, Vector3 heightmapScale, float[,] heightmap, bool[,] holes)
        {
            int vertexCount = width * height;
            var job = new ComputeTerrainMeshJob();
            job.heightmap = new NativeArray<float>(vertexCount, Allocator.Persistent);
            for (int i = 0; i < vertexCount; ++i)
                job.heightmap[i] = heightmap[i / (width), i % (width)];

            job.holes = new NativeArray<bool>((width - 1) * (height - 1), Allocator.Persistent);
            for (int i = 0; i < (width - 1) * (height - 1); ++i)
                job.holes[i] = holes[i / (width - 1), i % (width - 1)];

            job.width = width;
            job.height = height;
            job.heightmapScale = heightmapScale;

            job.positions = new NativeArray<float3>(vertexCount, Allocator.Persistent);
            job.uvs = new NativeArray<float2>(vertexCount, Allocator.Persistent);
            job.normals = new NativeArray<float3>(vertexCount, Allocator.Persistent);
            job.indices = new NativeArray<int>((width - 1) * (height - 1) * 6, Allocator.Persistent);

            JobHandle jobHandle = job.Schedule(vertexCount, math.max(width, 128));

            return new AsyncTerrainToMeshRequest(job, jobHandle);
        }

        static public AsyncTerrainToMeshRequest ConvertAsync(Terrain terrain)
        {
            TerrainData terrainData = terrain.terrainData;
            int width = terrainData.heightmapTexture.width;
            int height = terrainData.heightmapTexture.height;
            float[,] heightmap = terrain.terrainData.GetHeights(0, 0, width, height);
            bool[,] holes = terrain.terrainData.GetHoles(0, 0, width - 1, height - 1);
            return MakeAsyncTerrainToMeshRequest(width, height, terrainData.heightmapScale, heightmap, holes);
        }

        static public AsyncTerrainToMeshRequest ConvertAsync(int heightmapWidth, int heightmapHeight, short[] heightmapData, Vector3 heightmapScale, int holeWidth, int holeHeight, byte[] holedata)
        {
            float[,] heightmap = new float[heightmapWidth,heightmapHeight];
            for (int y = 0; y < heightmapHeight; ++y)
                for (int x = 0; x < heightmapWidth; ++x)
                    heightmap[y, x] = (float)heightmapData[y * heightmapWidth + x] / (float)32766;

            bool[,] holes = new bool[heightmapWidth - 1, heightmapHeight - 1];
            if (holedata != null)
            {
                for (int y = 0; y < heightmapHeight - 1; ++y)
                    for (int x = 0; x < heightmapWidth - 1; ++x)
                        holes[y, x] = holedata[y * holeWidth + x] != 0;
            }
            else
            {
                for (int y = 0; y < heightmapHeight - 1; ++y)
                    for (int x = 0; x < heightmapWidth - 1; ++x)
                        holes[x, y] = true;
            }
            return MakeAsyncTerrainToMeshRequest(heightmapWidth, heightmapHeight, heightmapScale, heightmap, holes);
        }
        static public Mesh Convert(Terrain terrain)
        {
            var request = ConvertAsync(terrain);
            request.WaitForCompletion();
            return request.GetMesh();
        }

        static public Mesh Convert(int heightmapWidth, int heightmapHeight, short[] heightmapData, Vector3 heightmapScale, int holeWidth, int holeHeight, byte[] holedata)
        {
            var request = ConvertAsync(heightmapWidth, heightmapHeight, heightmapData, heightmapScale, holeWidth, holeHeight, holedata);
            request.WaitForCompletion();
            return request.GetMesh();
        }
    }

    internal struct AsyncTerrainToMeshRequest
    {
        internal AsyncTerrainToMeshRequest(ComputeTerrainMeshJob job, JobHandle jobHandle)
        {
            m_Job = job;
            m_JobHandle = jobHandle;
        }

        public bool done { get { return m_JobHandle.IsCompleted; } }
        public Mesh GetMesh()
        {
            if (!done)
                return null;

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(m_Job.positions);
            mesh.SetUVs(0, m_Job.uvs);
            mesh.SetNormals(m_Job.normals);
            mesh.SetIndices(TriangleIndicesWithoutHoles().ToArray(), MeshTopology.Triangles, 0);

            m_Job.DisposeArrays();

            return mesh;
        }

        public void WaitForCompletion()
        {
            m_JobHandle.Complete();
        }

        List<int> TriangleIndicesWithoutHoles()
        {
            var trianglesWithoutHoles = new List<int>((m_Job.width - 1) * (m_Job.height - 1) * 6);
            for (int i = 0; i < m_Job.indices.Length; i += 3)
            {
                int i1 = m_Job.indices[i];
                int i2 = m_Job.indices[i + 1];
                int i3 = m_Job.indices[i + 2];

                if (i1 != 0 && i2 != 0 && i3 != 0)
                {
                    trianglesWithoutHoles.Add(i1);
                    trianglesWithoutHoles.Add(i2);
                    trianglesWithoutHoles.Add(i3);
                }
            }

            if (trianglesWithoutHoles.Count == 0)
            {
                trianglesWithoutHoles.Add(0);
                trianglesWithoutHoles.Add(0);
                trianglesWithoutHoles.Add(0);
            }

            return trianglesWithoutHoles;
        }

        JobHandle m_JobHandle;
        ComputeTerrainMeshJob m_Job;
    }

    [BurstCompile]
    internal struct ComputeTerrainMeshJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> heightmap;

        [ReadOnly]
        public NativeArray<bool> holes;

        public int width;
        public int height;
        public float3 heightmapScale;

        public NativeArray<float3> positions;
        public NativeArray<float2> uvs;
        public NativeArray<float3> normals;

        [NativeDisableParallelForRestriction]
        public NativeArray<int> indices;

        public void DisposeArrays()
        {
            heightmap.Dispose();
            holes.Dispose();
            positions.Dispose();
            uvs.Dispose();
            normals.Dispose();
            indices.Dispose();
        }

        public void Execute(int index)
        {
            int vertexIndex = index;
            int x = vertexIndex % width;
            int y = vertexIndex / height;

            float3 v = new float3(x, heightmap[y*width +x], y);

            positions[vertexIndex] = v * heightmapScale;
            uvs[vertexIndex] = v.xz / new float2(width, height);
            normals[vertexIndex] = CalculateTerrainNormal(heightmap, x, y, width, height, heightmapScale);

            if (x < width - 1 && y < height - 1)
            {
                int i1 = y * width + x;
                int i2 = i1 + 1;
                int i3 = i1 + width;
                int i4 = i3 + 1;

                int faceIndex = x + y * (width - 1);

                if (!holes[faceIndex])
                {
                    i1 = i2 = i3 = i4 = 0;
                }

                indices[6* faceIndex + 0] = i1;
                indices[6* faceIndex + 1] = i4;
                indices[6* faceIndex + 2] = i2;

                indices[6* faceIndex + 3] = i1;
                indices[6* faceIndex + 4] = i3;
                indices[6* faceIndex + 5] = i4;
            }
        }

        static float3 CalculateTerrainNormal(NativeArray<float> heightmap, int x, int y, int width, int height, float3 scale)
        {
            float dY, dX;

            dX = SampleHeight(x - 1, y - 1, width, height, heightmap, scale.y) * -1.0F;
            dX += SampleHeight(x - 1, y, width, height, heightmap, scale.y) * -2.0F;
            dX += SampleHeight(x - 1, y + 1, width, height, heightmap, scale.y) * -1.0F;
            dX += SampleHeight(x + 1, y - 1, width, height, heightmap, scale.y) * 1.0F;
            dX += SampleHeight(x + 1, y, width, height, heightmap, scale.y) * 2.0F;
            dX += SampleHeight(x + 1, y + 1, width, height, heightmap, scale.y) * 1.0F;

            dX /= scale.x;

            dY = SampleHeight(x - 1, y - 1, width, height, heightmap, scale.y) * -1.0F;
            dY += SampleHeight(x, y - 1, width, height, heightmap, scale.y) * -2.0F;
            dY += SampleHeight(x + 1, y - 1, width, height, heightmap, scale.y) * -1.0F;
            dY += SampleHeight(x - 1, y + 1, width, height, heightmap, scale.y) * 1.0F;
            dY += SampleHeight(x, y + 1, width, height, heightmap, scale.y) * 2.0F;
            dY += SampleHeight(x + 1, y + 1, width, height, heightmap, scale.y) * 1.0F;
            dY /= scale.z;

            // Cross Product of components of gradient reduces to
            return math.normalize(new float3(-dX, 8, -dY));
        }
        static float SampleHeight(int x, int y, int width, int height, NativeArray<float> heightmap, float scale)
        {
            x = math.clamp(x, 0, width - 1);
            y = math.clamp(y, 0, height - 1);
            return heightmap[x + y* width] * scale;
        }

    }

}
