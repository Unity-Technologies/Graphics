using System.Runtime.InteropServices;
using Unity.Collections;


namespace UnityEngine.Rendering.Universal
{
    internal class ShadowUtility
    {
        const int k_VerticesPerTriangle = 3;
        const int k_TrianglesPerEdge = 2;
        const int k_MinimumEdges = 3;

        static public int debugVert = 0;

        [StructLayout(LayoutKind.Sequential)]
        struct ShadowMeshVertex
        {
            public Vector3 position;
            public Vector4 tangent;

            public ShadowMeshVertex(Vector3 inPosition, Vector4 inTangent)
            {
                position = inPosition;
                tangent = inTangent;
            }
        }

        static VertexAttributeDescriptor[] m_VertexLayout = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Tangent,  VertexAttributeFormat.Float32, 4)
        };

        static void CalculateTangents(ref NativeArray<Vector2> inVertices, ref NativeArray<ShadowShape2D.Edge> inEdges, ref NativeArray<Vector4> outTangents)
        {
            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                Vector3 start = inVertices[v0];
                Vector3 end = inVertices[v1];

                outTangents[v0] = new Vector4(0,0, end.x, end.y);

                Vector4 tangent = Vector3.Cross(Vector3.Normalize(end - start), -Vector3.forward).normalized;

                int additionalVerticesStart = 2 * i + inVertices.Length;
                outTangents[additionalVerticesStart] = new Vector4(tangent.x, tangent.y, end.x, end.y);
                outTangents[additionalVerticesStart + 1] = new Vector4(tangent.x, tangent.y, start.x, start.y);
            }
        }

        static void CalculateContraction(ref NativeArray<Vector2> inVertices, ref NativeArray<ShadowShape2D.Edge> inEdges, ref NativeArray<Vector4> inTangents, float contractionDistance, ref NativeArray<Vector3> outReducedVertices)
        {
            // Reduce the original verices
            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;
                int v2 = inEdges[(i + 1) % inEdges.Length].v1;

                Vector3 pt0 = inVertices[v0];
                Vector3 pt1 = inVertices[v1];
                Vector3 pt2 = inVertices[v2];

                Vector3 tangent0 = Vector3.Cross(Vector3.Normalize(pt1-pt0), -Vector3.forward).normalized;
                Vector3 tangent1 = Vector3.Cross(Vector3.Normalize(pt2-pt1), -Vector3.forward).normalized;

                Vector3 reductionDir = (-0.5f * (tangent0 + tangent1)).normalized;

                outReducedVertices[v1] = contractionDistance * reductionDir + (Vector3)inVertices[v1];
            }
        }

        static void CalculateVertices(ref NativeArray<Vector3> inVertices, ref NativeArray<ShadowShape2D.Edge> inEdges, ref NativeArray<Vector4> inTangents, ref NativeArray<ShadowMeshVertex> outMeshVertices)
        {
            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                ShadowMeshVertex originalShadowMesh = new ShadowMeshVertex(inVertices[v0], inTangents[v0]);
                outMeshVertices[v0] = originalShadowMesh;

                int additionalVerticesStart = 2 * i + inVertices.Length;
                ShadowMeshVertex additionalVertex0 = new ShadowMeshVertex(inVertices[v0], inTangents[additionalVerticesStart]);
                ShadowMeshVertex additionalVertex1 = new ShadowMeshVertex(inVertices[v1], inTangents[additionalVerticesStart + 1]);

                outMeshVertices[additionalVerticesStart] = additionalVertex0;
                outMeshVertices[additionalVerticesStart + 1] = additionalVertex1;
            }
        }

        static void CalculateTriangles(ref NativeArray<Vector2> inVertices, ref NativeArray<ShadowShape2D.Edge> inEdges, ref NativeArray<int> meshIndices)
        {
            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                int additionalVerticesStart = 2 * i + inVertices.Length;

                int startingMeshIndex = k_VerticesPerTriangle * k_TrianglesPerEdge * i; 
                meshIndices[startingMeshIndex] = (ushort)v0;
                meshIndices[startingMeshIndex + 1] = (ushort)additionalVerticesStart;
                meshIndices[startingMeshIndex + 2] = (ushort)(additionalVerticesStart + 1);
                meshIndices[startingMeshIndex + 3] = (ushort)(additionalVerticesStart + 1);
                meshIndices[startingMeshIndex + 4] = (ushort)v1;
                meshIndices[startingMeshIndex + 5] = (ushort)v0;
            }
        }

        static BoundingSphere CalculateBoundingSphere(ref NativeArray<Vector3> inVertices)
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
        static public BoundingSphere GenerateShadowMesh(Mesh mesh, NativeArray<Vector2> inVertices, NativeArray<ShadowShape2D.Edge> inEdges, float contractionDistance)
        {
            Debug.AssertFormat(inEdges.Length >= k_MinimumEdges, "Shadow shape path must have 3 or more edges");

            // Setup our buffers
            int meshVertexCount = inVertices.Length + 2 * inEdges.Length;                       // Each vertex will have a duplicate that can be extruded.
            int meshIndexCount = inEdges.Length * k_VerticesPerTriangle * k_TrianglesPerEdge;  // There are two triangles per edge making a degenerate rectangle (0 area)

            NativeArray<Vector3> meshReducedVertices = new NativeArray<Vector3>(inVertices.Length, Allocator.Temp);
            NativeArray<Vector4> meshTangents = new NativeArray<Vector4>(meshVertexCount, Allocator.Temp);
            NativeArray<int> meshIndices = new NativeArray<int>(meshIndexCount, Allocator.Temp);
            NativeArray<ShadowMeshVertex> meshFinalVertices = new NativeArray<ShadowMeshVertex>(meshVertexCount, Allocator.Temp);

            // Get vertex reduction directions
            CalculateTangents(ref inVertices, ref inEdges, ref meshTangents);                            // meshVertices contain a normal component
            CalculateContraction(ref inVertices, ref inEdges, ref meshTangents, contractionDistance, ref meshReducedVertices);
            CalculateVertices(ref meshReducedVertices, ref inEdges, ref meshTangents, ref meshFinalVertices);
            CalculateTriangles(ref inVertices, ref inEdges, ref meshIndices);

            // Set the mesh data
            mesh.SetVertexBufferParams(meshVertexCount, m_VertexLayout);
            mesh.SetVertexBufferData<ShadowMeshVertex>(meshFinalVertices, 0, 0, meshVertexCount);
            mesh.SetIndexBufferParams(meshIndexCount, IndexFormat.UInt32);
            mesh.SetIndexBufferData<int>(meshIndices, 0, 0, meshIndexCount);

            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, meshIndexCount));
    

            BoundingSphere retBoundingSphere = CalculateBoundingSphere(ref meshReducedVertices);

            meshTangents.Dispose();
            meshReducedVertices.Dispose();
            meshIndices.Dispose();
            meshFinalVertices.Dispose();

            return retBoundingSphere;
        }
    }
}
