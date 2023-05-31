using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SetSRPBatcher : MonoBehaviour
{
    private bool previousState = true;

    // Save previous state and set after one frame delay
    public void SetBatching( bool state )
    {
        previousState = GraphicsSettings.useScriptableRenderPipelineBatching;
        StartCoroutine(SetBatchingDelayed(state));
    }

    private IEnumerator SetBatchingDelayed(bool state)
    {
        yield return null;

        GraphicsSettings.useScriptableRenderPipelineBatching = state;
    }

    // Reset to previous state when the script is destroyed (scene unload in particular)
    private void OnDestroy()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = previousState;
    }
}
