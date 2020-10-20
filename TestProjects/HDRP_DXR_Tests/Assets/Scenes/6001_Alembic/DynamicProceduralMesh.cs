using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
class DynamicProceduralMesh : MonoBehaviour
{
    Mesh _mesh;

    private uint minSphereRes = 4;
    private uint maxSphereRes = 16;
    private uint currSphereRes;

    void UpdateMeshGeometry(uint sphereRes)
    {
        using (var varray = CreateVertexArray(sphereRes))
        {
            _mesh.SetVertexBufferParams
              (varray.Length,
               new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
               new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3));
            _mesh.SetVertexBufferData(varray, 0, 0, varray.Length);
        }

        using (var iarray = CreateIndexArray(sphereRes))
        {
            _mesh.SetIndexBufferParams(iarray.Length, IndexFormat.UInt32);
            _mesh.SetIndexBufferData(iarray, 0, 0, iarray.Length);
            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, iarray.Length));
        }

        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2);

        GetComponent<MeshFilter>().sharedMesh = _mesh;
    }

    void OnEnable()
    {
        _mesh = new Mesh();

        _mesh.MarkDynamic();

        currSphereRes = minSphereRes;
        UpdateMeshGeometry(currSphereRes);
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            
            currSphereRes = currSphereRes < maxSphereRes ? currSphereRes + 1 : minSphereRes;
            UpdateMeshGeometry(currSphereRes);
        }
    }

    void OnDestroy() => DestroyImmediate(_mesh);

    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
    }

    NativeArray<Vertex> CreateVertexArray(uint sphereRes)
    { 
        uint vertexCount = (sphereRes + 1) * (sphereRes + 1);

        Vertex[] vertices = new Vertex[vertexCount];

        float sectorStep = 2 * Mathf.PI / sphereRes;
        float stackStep = Mathf.PI / sphereRes;

        int vertexIndex = 0;
        for (uint i = 0; i <= sphereRes; i++)
        {
            float stackAngle = Mathf.PI / 2 - i * stackStep;

            float xz = Mathf.Cos(stackAngle);
            float y = Mathf.Sin(stackAngle);

            for (uint j = 0; j <= sphereRes; j++)
            {
                float sectorAngle = j * sectorStep;

                float x = xz * Mathf.Cos(sectorAngle); 
                float z = xz * Mathf.Sin(sectorAngle);

                vertices[vertexIndex].position = new Vector3(x, y, z);
                vertices[vertexIndex].normal = new Vector3(x, y, z);

                vertexIndex++;
            }
        }

        return new NativeArray<Vertex>(vertices, Allocator.Temp);
    }

    NativeArray<uint> CreateIndexArray(uint sphereRes)
    {
        uint indexCount = 2 * 3 * sphereRes * sphereRes;
        var indices = new uint[indexCount];

        int index = 0;

        for (uint i = 0; i < sphereRes; i++)
            for (uint j = 0; j < sphereRes; j++)
            {
                indices[index++] = i * (sphereRes + 1) + j;
                indices[index++] = (i + 1) * (sphereRes + 1) + j + 1;
				indices[index++] = (i + 1) * (sphereRes + 1) + j;

                indices[index++] = i * (sphereRes + 1) + j;
                indices[index++] = i * (sphereRes + 1) + j + 1;
				indices[index++] = (i + 1) * (sphereRes + 1) + j + 1;
            }

        return new NativeArray<uint>(indices, Allocator.Temp);
    }
}
