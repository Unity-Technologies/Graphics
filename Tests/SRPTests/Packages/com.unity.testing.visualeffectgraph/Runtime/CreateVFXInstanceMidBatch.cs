using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class CreateVFXInstanceMidBatch : MonoBehaviour
{
    public GameObject Prefab;
    public int EffectCount = 0;

    private GameObject[] gameObjects;

    private IEnumerator Start()
    {
        if (Prefab.TryGetComponent(out VisualEffect vfx))
        {
            gameObjects = new GameObject[EffectCount];

            // Create some vfx instances
            for (int i = 0; i < EffectCount; ++i)
            {
                gameObjects[i] = Instantiate(Prefab, transform);
            }

            yield return new WaitForEndOfFrame();

            // Destroy one instance in the middle
            Destroy(gameObjects[EffectCount / 2]);

            yield return new WaitForEndOfFrame();

            // Spawn a new VFX, potentially reusing the free instance
            Instantiate(Prefab, transform);

            // Remove all the other instances
            for (int i = 0; i < EffectCount; ++i)
            {
                if (gameObjects[i] != null)
                {
                    Destroy(gameObjects[i]);
                }
            }
        }
    }
}
