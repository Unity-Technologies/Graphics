using UnityEngine;

class MeshGenerator : MonoBehaviour
{
    void Start()
    {
        var vertices = new [] {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(+0.5f, -0.5f, 0),
            new Vector3(-0.5f, +0.5f, 0),
            new Vector3(+0.5f, +0.5f, 0)
        };

        var normals = new [] {
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1)
        };

        var uvs_0 = new [] {
            new Vector4(0, 0, 0, 0),
            new Vector4(1, 0, 1, 0),
            new Vector4(0, 1, 0, 1),
            new Vector4(1, 1, 1, 1)
        };
		
		var uvs_1 = new [] {
            new Vector4(0, 0, 0, 0),
            new Vector4(0.66f, 0, 0.66f, 0),
            new Vector4(0, 0.66f, 0, 0.66f),
            new Vector4(0.66f, 0.66f, 0.66f, 0.66f)
        };
		
		var uvs_2 = new [] {
            new Vector4(0, 0, 0, 0),
            new Vector4(0.33f, 0, 0.33f, 0),
            new Vector4(0, 0.33f, 0, 0.33f),
            new Vector4(0.33f, 0.33f, 0.33f, 0.33f)
        };
		
		var uvs_3 = new [] {
            new Vector4(0, 0, 0, 0),
            new Vector4(0.16f, 0, 0.16f, 0),
            new Vector4(0, 0.16f, 0, 0.16f),
            new Vector4(0.16f, 0.16f, 0.16f, 0.16f)
        };

        var indices = new [] { 0, 2, 1, 2, 3, 1 };

        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs_0);
		mesh.SetUVs(1, uvs_1);
		mesh.SetUVs(2, uvs_2);
		mesh.SetUVs(3, uvs_3);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}
