using System.Collections;
using UnityEngine;

public class SetupDynamicResolution : MonoBehaviour
{
    [SerializeField] private float scale = 0.5f;

    IEnumerator Start()
    {
        yield return null;
        ScalableBufferManager.ResizeBuffers(scale, scale);
    }

    private void OnDestroy()
    {
        ScalableBufferManager.ResizeBuffers(1, 1);
    }
}
