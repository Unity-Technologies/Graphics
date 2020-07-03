using UnityEngine;

public class CreateStaticBatch : MonoBehaviour
{
    public int count;
    public Mesh mesh;
    public Material mat;

    public void Start()
    {
        var i = 0;
        while (i < count)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (mesh != null)
            {
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
            }
            if (mat != null)
            {
                go.GetComponent<Renderer>().material = mat;
            }
            else
            {
                go.GetComponent<Renderer>().material.SetFloat("Smoothness", 1.0f); // existing template images are made for 0 gloss
            }
            go.transform.parent = transform;
            go.transform.localPosition = new Vector3(i * 1.2f, 0, 0);
            go.transform.localRotation = Quaternion.identity;
            ++i;
        }
        StaticBatchingUtility.Combine(gameObject);
    }

    public CreateStaticBatch()
    {
        count = 2;
    }
}
