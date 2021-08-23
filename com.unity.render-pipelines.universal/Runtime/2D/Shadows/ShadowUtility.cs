using System.Runtime.InteropServices;
using Unity.Collections;


namespace UnityEngine.Rendering.Universal
{
    internal class ShadowUtility
    {
        const int k_VerticesPerTriangle = 3;
        const int k_TrianglesPerEdge = 2;
        const int k_MinimumEdges = 3;

        [StructLayout(LayoutKind.Sequential)]
        struct ShadowMeshVertex
        {
            public Vector3 position;
            public Vector4 normal;

            public ShadowMeshVertex(Vector3 inPosition, Vector4 inNormal)
            {
                position = inPosition;
                normal = inNormal;
            }
        }

        static VertexAttributeDescriptor[] m_VertexLayout = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 4)
        };

        void CalculateNormals(ref NativeArray<Vector2> inVertices, ref NativeArray<ShadowShape2D.Edge> inEdges, ref NativeArray<Vector4> outNormals)
        {
            for (int i = 0; i < inVertices.Length; i++)
            {
                outNormals[i] = Vector4.zero;
            }

            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                Vector3 start = inVertices[v0];
                Vector3 end = inVertices[v1];

                Vector4 normal = Vector3.Cross(Vector3.Normalize(end - start), -Vector3.forward).normalized;

                int additionalVerticesStart = 2 * i + inVertices.Length;
                outNormals[additionalVerticesStart] = new Vector4(normal.x, normal.y, end.x, end.y);
                outNormals[additionalVerticesStart + 1] = new Vector4(normal.x, normal.y, start.x, start.y);
            }
        }

        void CalculateReduction(ref NativeArray<Vector2> inVertices, ref NativeArray<ShadowShape2D.Edge> inEdges, ref NativeArray<Vector4> inNormals, float reductionDistance, ref NativeArray<Vector3> outReducedVertices)
        {
            // Reduce the original verices
            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                int normal0Index = 2 * i + inVertices.Length;
                int normal1Index = (2 * i + 2) % inEdges.Length + inVertices.Length;

                Vector3 normal0 = inNormals[normal0Index];
                Vector3 normal1 = inNormals[normal1Index];

                Vector3 reductionDir = (-0.5f * (normal0 + normal1)).normalized;

                outReducedVertices[v1] = reductionDistance * reductionDir + (Vector3)inVertices[v1];
            }
        }

        void CalculateVertices(ref NativeArray<Vector3> inVertices, ref NativeArray<ShadowShape2D.Edge> inEdges, ref NativeArray<Vector4> inNormals, ref NativeArray<ShadowMeshVertex> outMeshVertices)
        {
            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                ShadowMeshVertex originalShadowMesh = new ShadowMeshVertex(inVertices[v1], inNormals[v1]);
                outMeshVertices[v1] = originalShadowMesh;

                int additionalVerticesStart = 2 * i + inVertices.Length;
                ShadowMeshVertex additionalVertex0 = new ShadowMeshVertex(inVertices[v0], inNormals[additionalVerticesStart]);
                ShadowMeshVertex additionalVertex1 = new ShadowMeshVertex(inVertices[v1], inNormals[additionalVerticesStart]);

                outMeshVertices[additionalVerticesStart] = additionalVertex0;
                outMeshVertices[additionalVerticesStart + 1] = additionalVertex1;
            }
        }

        void CalculateTriangles(ref NativeArray<Vector2> inVertices, ref NativeArray<ShadowShape2D.Edge> inEdges, ref NativeArray<ushort> meshIndices)
        {
            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                int additionalVerticesStart = 2 * i + inVertices.Length;

                int startingMeshIndex = 6 * i;
                meshIndices[i] = (ushort)v0;
                meshIndices[i + 1] = (ushort)additionalVerticesStart;
                meshIndices[i + 2] = (ushort)(additionalVerticesStart + 1);
                meshIndices[i + 3] = (ushort)(additionalVerticesStart + 1);
                meshIndices[i + 4] = (ushort)v1;
                meshIndices[i + 5] = (ushort)v0;
            }
        }

        BoundingSphere CalculateBoundingSphere(ref NativeArray<Vector3> inVertices)
        {
            Debug.AssertFormat(inVertices.Length > 0, "At least one vertex is required to calculate a bounding sphere");

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            // Add outline vertices
            for (int i = 0; i < inVertices.Length; i++)
            {
                Vector3 vertex = inVertices[i];

                if (minX > vertex.x)
                    minX = vertex.x;
                if (maxX < vertex.x)
                    maxX = vertex.x;

                if (minY > vertex.y)
                    minY = vertex.y;
                if (maxY < vertex.y)
                    maxY = vertex.y;
            }

            // Calculate bounding sphere (circle)
            Vector3 origin = new Vector2(0.5f * (minX + maxX), 0.5f * (minY + maxY));
            float deltaX = maxX - minX;
            float deltaY = maxY - minY;
            float radius = 0.5f * Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

            return new BoundingSphere(origin, radius);
        }


        // inEdges is expected to be contiguous
        public BoundingSphere GenerateShadowMesh(Mesh mesh, NativeArray<Vector2> inVertices, NativeArray<ShadowShape2D.Edge> inEdges, float reductionDistance)
        {
            Debug.AssertFormat(inEdges.Length >= k_MinimumEdges, "Shadow shape path must have 3 or more edges");

            // Setup our buffers
            int meshVertexCount = inVertices.Length + 2 * inEdges.Length;                       // Each vertex will have a duplicate that can be extruded.
            int meshIndexCount = inEdges.Length * k_VerticesPerTriangle * k_TrianglesPerEdge;  // There are two triangles per edge making a degenerate rectangle (0 area)

            NativeArray<Vector3> meshReducedVertices = new NativeArray<Vector3>(inVertices.Length, Allocator.Temp);
            NativeArray<Vector4> meshNormals = new NativeArray<Vector4>(meshVertexCount, Allocator.Temp);
            NativeArray<ushort> meshIndices = new NativeArray<ushort>(meshIndexCount, Allocator.Temp);
            NativeArray<ShadowMeshVertex> meshFinalVertices = new NativeArray<ShadowMeshVertex>(meshVertexCount, Allocator.Temp);

            // Get vertex reduction directions
            CalculateNormals(ref inVertices, ref inEdges, ref meshNormals);                            // meshVertices contain a normal component
            CalculateReduction(ref inVertices, ref inEdges, ref meshNormals, reductionDistance, ref meshReducedVertices);
            CalculateVertices(ref meshReducedVertices, ref inEdges, ref meshNormals, ref meshFinalVertices);
            CalculateTriangles(ref inVertices, ref inEdges, ref meshIndices);

            // Set the mesh data
            mesh.SetVertexBufferParams(meshVertexCount, m_VertexLayout);
            mesh.SetVertexBufferData<ShadowMeshVertex>(meshFinalVertices, 0, 0, meshVertexCount);
            mesh.SetIndexBufferParams(meshIndexCount, IndexFormat.UInt16);
            mesh.SetIndexBufferData<ushort>(meshIndices, 0, 0, meshIndexCount);

            BoundingSphere retBoundingSphere = CalculateBoundingSphere(ref meshReducedVertices);

            meshNormals.Dispose();
            meshReducedVertices.Dispose();
            meshIndices.Dispose();
            meshFinalVertices.Dispose();

            return retBoundingSphere;
        }
    }
}
