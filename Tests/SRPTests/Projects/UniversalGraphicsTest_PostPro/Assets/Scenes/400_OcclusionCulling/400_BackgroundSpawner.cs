using UnityEngine;

public class BackgroundSpawner : MonoBehaviour
{
    public GameObject objectToSpawn;

    public void Awake()
    {
        if (objectToSpawn is null)
            return;

        var parentTransform = transform;
        var localRotation = Quaternion.AngleAxis(30.0f, Vector3.up) * Quaternion.AngleAxis(30.0f, Vector3.right);
        var spacing = 1.5f;
        for (int iy = -9; iy <= 9; ++iy)
        {
            for (int ix = -16; ix <= 16; ++ix)
            {
                Object.Instantiate(objectToSpawn, new Vector3(ix * spacing, iy * spacing, 0.0f), localRotation, parentTransform);
            }
        }
    }
}
