using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DisableBatching : MonoBehaviour
{
    public void ToggleBatching()
    {
        Invoke("Batch", 0.1f); //function is invoked due to batching toggle issue not getting executed on start
    }

    private void Batch()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = !GraphicsSettings.useScriptableRenderPipelineBatching;
    }
}
