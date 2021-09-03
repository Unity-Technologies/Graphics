using System.Runtime.InteropServices;
using Unity.Collections;


namespace UnityEngine.Rendering.Universal
{
    internal class ShadowUtility
    {
        const int k_AdditionalVerticesPerVertex = 2;
        const int k_VerticesPerTriangle = 3;
        const int k_TrianglesPerEdge = 3;
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


        static void CalculateTangents(NativeArray<Vector3> inVertices, NativeArray<ShadowMesh2D.Edge> inEdges, NativeArray<Vector3> contractionDirection, ref NativeArray<Vector4> outTangents)
        {
            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                Vector3 start = inVertices[v0];
                Vector3 end = inVertices[v1];

                Vector3 contractDir0 = contractionDirection[v0];
                Vector3 contractDir1 = contractionDirection[v1];

                outTangents[v0] = new Vector4(contractDir0.x, contractDir0.y, end.x, end.y);

                int additionalVerticesStart = 2 * i + inVertices.Length;
                outTangents[additionalVerticesStart] = new Vector4(contractDir0.x, contractDir0.y, end.x, end.y);
                outTangents[additionalVerticesStart + 1] = new Vector4(contractDir1.x, contractDir1.y, start.x, start.y);
            }
        }

        static void CalculateContraction(NativeArray<Vector3> inVertices, NativeArray<ShadowMesh2D.Edge> inEdges, NativeArray<Vector4> inTangents, NativeArray<int> inShapeStartingIndices, ref NativeArray<Vector3> outContractionDirection)
        {
            for (int shapeIndex = 0; shapeIndex < inShapeStartingIndices.Length; shapeIndex++)
            {
                int startingIndex = inShapeStartingIndices[shapeIndex];

                if (startingIndex < 0)
                    return;

                int nextStartingIndex = -1;
                if(shapeIndex + 1 < inShapeStartingIndices.Length)
                    nextStartingIndex = inShapeStartingIndices[shapeIndex + 1];

                int numEdgesInCurShape = nextStartingIndex >= 0 ? nextStartingIndex - startingIndex : inEdges.Length - startingIndex;

                int edgeIndex = startingIndex;
                for (int i = 0; i < numEdgesInCurShape; i++)
                {
                    int curEdge = startingIndex + i;
                    int nextEdge = i + 1 < numEdgesInCurShape ? startingIndex + i + 1 : startingIndex;

                    int v0 = inEdges[curEdge].v0;
                    int v1 = inEdges[curEdge].v1;
                    int v2 = inEdges[nextEdge].v1;

                    Vector3 pt0 = inVertices[v0];
                    Vector3 pt1 = inVertices[v1];
                    Vector3 pt2 = inVertices[v2];

                    Vector3 tangent0 = Vector3.Cross(Vector3.Normalize(pt1 - pt0), -Vector3.forward).normalized;
                    Vector3 tangent1 = Vector3.Cross(Vector3.Normalize(pt2 - pt1), -Vector3.forward).normalized;

                    outContractionDirection[v1] = (-0.5f * (tangent0 + tangent1)).normalized;
                }
            }
        }

        static void ApplyContraction(NativeArray<Vector3> inVertices, NativeArray<ShadowMesh2D.Edge> inEdges, NativeArray<Vector3> inContractionDirection, float inContractionDistance, ref NativeArray<Vector3> outVertices)
        {
            for(int i=0;i<inEdges.Length;i++)
            {
                int index = inEdges[i].v0;
                outVertices[i] = inVertices[index];
            }
        }

        static void CalculateVertices(NativeArray<Vector3> inVertices, NativeArray<ShadowMesh2D.Edge> inEdges, NativeArray<Vector4> inTangents, ref NativeArray<ShadowMeshVertex> outMeshVertices)
        {
            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                Vector4 pt0 = inVertices[v0];
                Vector4 pt1 = inVertices[v1];

                pt0.z = 1;  // for the original mesh this value is one as it cannot be stretched
                ShadowMeshVertex originalShadowMesh = new ShadowMeshVertex(pt0, inTangents[v0]);
                outMeshVertices[v0] = originalShadowMesh;

                pt0.z = 0;
                pt1.z = 0;
                int additionalVerticesStart = 2 * i + inVertices.Length;
                ShadowMeshVertex additionalVertex0 = new ShadowMeshVertex(pt0, inTangents[additionalVerticesStart]);
                ShadowMeshVertex additionalVertex1 = new ShadowMeshVertex(pt1, inTangents[additionalVerticesStart + 1]);

                outMeshVertices[additionalVerticesStart] = additionalVertex0;
                outMeshVertices[additionalVerticesStart + 1] = additionalVertex1;
            }
        }

        static void CalculateTriangles(NativeArray<Vector3> inVertices, NativeArray<ShadowMesh2D.Edge> inEdges, NativeArray<int> inShapeStartingIndices, ref NativeArray<int> outMeshIndices)
        {
            for (int shapeIndex = 0; shapeIndex < inShapeStartingIndices.Length; shapeIndex++)
            {
                int startingIndex = inShapeStartingIndices[shapeIndex];
                if (startingIndex < 0)
                    return;

                int endIndex = inEdges.Length;
                if ((shapeIndex + 1) < inShapeStartingIndices.Length && inShapeStartingIndices[shapeIndex + 1] > -1)
                    endIndex = inShapeStartingIndices[shapeIndex + 1];

                int prevEdge = endIndex - 1;
                for (int i = startingIndex; i < endIndex; i++)
                {
                    int v0 = inEdges[i].v0;
                    int v1 = inEdges[i].v1;


                    int additionalVerticesStart = k_AdditionalVerticesPerVertex * i + inVertices.Length;

                    int startingMeshIndex = k_VerticesPerTriangle * k_TrianglesPerEdge * i;
                    // Add a degenerate rectangle 
                    outMeshIndices[startingMeshIndex] = (ushort)v0;
                    outMeshIndices[startingMeshIndex + 1] = (ushort)additionalVerticesStart;
                    outMeshIndices[startingMeshIndex + 2] = (ushort)(additionalVerticesStart + 1);
                    outMeshIndices[startingMeshIndex + 3] = (ushort)(additionalVerticesStart + 1);
                    outMeshIndices[startingMeshIndex + 4] = (ushort)v1;
                    outMeshIndices[startingMeshIndex + 5] = (ushort)v0;

                    // Add a triangle to connect that rectangle to the neighboring rectangle so we have a seamless mesh
                    int prevAdditionalVertex = k_AdditionalVerticesPerVertex * prevEdge + inVertices.Length;
                    outMeshIndices[startingMeshIndex + 6] = (ushort)(additionalVerticesStart);
                    outMeshIndices[startingMeshIndex + 7] = (ushort)v0;
                    outMeshIndices[startingMeshIndex + 8] = (ushort)prevAdditionalVertex;

                    prevEdge = i;
                }
            }
        }

        static BoundingSphere CalculateBoundingSphere(NativeArray<Vector3> inVertices)
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
        static public BoundingSphere GenerateShadowMesh(Mesh mesh, NativeArray<Vector3> inVertices, NativeArray<ShadowMesh2D.Edge> inEdges, NativeArray<int> inShapeStartingIndices)
        {
            Debug.AssertFormat(inEdges.Length >= k_MinimumEdges, "Shadow shape path must have 3 or more edges");

            // Setup our buffers
            int meshVertexCount = inVertices.Length + 2 * inEdges.Length;                       // Each vertex will have a duplicate that can be extruded.
            int meshIndexCount = inEdges.Length * k_VerticesPerTriangle * k_TrianglesPerEdge;  // There are two triangles per edge making a degenerate rectangle (0 area)

            NativeArray<Vector3> contractionDirection = new NativeArray<Vector3>(inVertices.Length, Allocator.Temp);
            NativeArray<Vector4> meshTangents = new NativeArray<Vector4>(meshVertexCount, Allocator.Temp);
            NativeArray<int> meshIndices = new NativeArray<int>(meshIndexCount, Allocator.Temp);
            NativeArray<ShadowMeshVertex> meshFinalVertices = new NativeArray<ShadowMeshVertex>(meshVertexCount, Allocator.Temp);

            // Get vertex reduction directions
            CalculateContraction(inVertices, inEdges, meshTangents, inShapeStartingIndices, ref contractionDirection);
            CalculateTangents(inVertices, inEdges, contractionDirection, ref meshTangents);                            // meshVertices contain a normal component
            CalculateVertices(inVertices, inEdges, meshTangents, ref meshFinalVertices);
            CalculateTriangles(inVertices, inEdges, inShapeStartingIndices, ref meshIndices);

            // Set the mesh data
            mesh.SetVertexBufferParams(meshVertexCount, m_VertexLayout);
            mesh.SetVertexBufferData<ShadowMeshVertex>(meshFinalVertices, 0, 0, meshVertexCount);
            mesh.SetIndexBufferParams(meshIndexCount, IndexFormat.UInt32);
            mesh.SetIndexBufferData<int>(meshIndices, 0, 0, meshIndexCount);

            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, meshIndexCount));

            BoundingSphere retBoundingSphere = CalculateBoundingSphere(inVertices);

            contractionDirection.Dispose();
            meshTangents.Dispose();
            meshIndices.Dispose();
            meshFinalVertices.Dispose();

            return retBoundingSphere;
        }
    }
}
