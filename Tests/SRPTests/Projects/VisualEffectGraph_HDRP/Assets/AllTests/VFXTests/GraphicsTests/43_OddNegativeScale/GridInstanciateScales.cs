using UnityEngine;
using UnityEngine.VFX;

public class GridInstantiateScales : MonoBehaviour
{
    public GameObject prefab = null;
    public Vector3 separation = Vector3.one;

    void Start()
    {
        if (prefab != null)
        {

            for (int i = 0; i < 8; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    Vector3 particleScale = new Vector3();
                    particleScale.x = (i & 1) != 0 ? -1 : 1;
                    particleScale.y = (i & 2) >> 1 != 0 ? -1 : 1;
                    particleScale.z = (i & 4) >> 2 != 0 ? -1 : 1;
                    Vector3 effectScale = new Vector3();
                    effectScale.x = (j & 1) != 0 ? -1 : 1;
                    effectScale.y = (j & 2) >> 1 != 0 ? -1 : 1;
                    effectScale.z = (j & 4) >> 2 != 0 ? -1 : 1;
                    Vector3 position = new Vector3(i * separation.x, j * separation.y, 0);
                    GameObject instance = Instantiate(prefab, transform.TransformPoint(position), transform.rotation, transform);
                    instance.transform.localScale = Vector3.Scale(effectScale,instance.transform.localScale);
                    instance.GetComponent<VisualEffect>().SetVector3("ParticleScale", particleScale);
                }
            }
        }
    }
}
