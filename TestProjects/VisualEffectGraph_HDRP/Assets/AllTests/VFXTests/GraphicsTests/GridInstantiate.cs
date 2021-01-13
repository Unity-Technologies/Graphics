using UnityEngine;

public class GridInstantiate : MonoBehaviour
{
    public GameObject prefab = null;

    public Vector3Int elements = Vector3Int.one;

    public Vector3 offset = Vector3.zero;
    public Vector3 separation = Vector3.one;

    void Start()
    {
        Vector3 startOffset = offset - new Vector3(elements.x * separation.x, elements.y * separation.y, elements.z * separation.z) * 0.5f + 0.5f * Vector3.one;
        for (int i = 0; i < elements.x; ++i)
        {
            for (int j = 0; j < elements.y; ++j)
            {
                for (int k = 0; k < elements.z; ++k)
                {
                    Vector3 position = startOffset + new Vector3(i * separation.x, j * separation.y, k * separation.z);
                    Instantiate(prefab, transform.TransformPoint(position), transform.rotation, transform);
                }
            }
        }
    }
}
