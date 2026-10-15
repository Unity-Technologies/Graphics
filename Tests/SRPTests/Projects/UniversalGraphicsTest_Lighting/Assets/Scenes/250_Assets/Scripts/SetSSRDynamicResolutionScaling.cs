using System.Collections;
using UnityEngine;

public sealed class SetSSRDynamicResolutionScaling : MonoBehaviour
{
    const float k_Scale = 0.25f;

    void Awake()
    {
        GetComponent<Camera>().allowDynamicResolution = true;
    }

    IEnumerator Start()
    {
        yield return null;
        ScalableBufferManager.ResizeBuffers(k_Scale, k_Scale);

        if (!Mathf.Approximately(ScalableBufferManager.widthScaleFactor, k_Scale)
            || !Mathf.Approximately(ScalableBufferManager.heightScaleFactor, k_Scale))
        {
            throw new System.InvalidOperationException("Hardware dynamic resolution scaling was not applied.");
        }
    }

    void OnDestroy()
    {
        ScalableBufferManager.ResizeBuffers(1.0f, 1.0f);
    }
}
