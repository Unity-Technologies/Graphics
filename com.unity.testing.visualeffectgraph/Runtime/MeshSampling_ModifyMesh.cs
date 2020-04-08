using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshSampling_ModifyMesh : MonoBehaviour
{
    public Mesh mesh;
    private float time = 0.0f;
    private bool modified = false;

    void Start()
    {
        time = 0.0f;
        modified = false;
    }

    void Update()
    {
        time += Time.deltaTime;
        if (!modified && time > 2.0f)
        {
            if (mesh)
            {
                var reference = new Vector3[]
                {
                    new Vector3(-1.0f, 1.0f, 0.0f),
                    new Vector3(1.0f, 1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f, -1.0f, 0.0f),
                };

                var newVertices = new Vector3[mesh.vertexCount];
                for (int i = 0; i < mesh.vertexCount; ++i)
                {
                    newVertices[i] = reference[i % reference.Length] * 1.5f;
                }
                mesh.vertices = newVertices;
            }
            modified = true;
        }
    }
}
