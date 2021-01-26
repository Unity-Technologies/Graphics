using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
class ProceduralMesh : MonoBehaviour
{
    Mesh _mesh;

    private uint sphereRes = 128;

    void CreateMeshGeometry()
    {
        using (var varray = CreateVertexArray(sphereRes))
        {
            _mesh.SetVertexBufferParams
              (varray.Length,
               new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
               new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.SNorm16, 4),               
               new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
               new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2));

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

        CreateMeshGeometry();
    }

    void OnDestroy() => DestroyImmediate(_mesh);

    struct Vertex
    {
        public Vector3 position;
        public SNorm16x4 normal;
        public uint color;
        public Float16x2 texCoord;
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

                Vector3 pos = new Vector3(x, y, z);

                vertices[vertexIndex].position = pos;
                vertices[vertexIndex].normal = new SNorm16x4(x, y, z, 0.0f);
                vertices[vertexIndex].texCoord = new Float16x2(i / (float)(sphereRes), j / (float)(sphereRes));

                Vector3 color = Vector3.Normalize(pos);
                color *= 0.5f;
                color += new Vector3(0.5f, 0.5f, 0.5f);
                color *= 255.0f;
                uint colorRGBA = (uint)color.x | ((uint)color.y << 8) | ((uint)color.z << 16) | 0xff000000;
                vertices[vertexIndex].color = colorRGBA;                

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
