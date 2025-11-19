using UnityEngine;
using UnityEngine.Rendering.Universal;

public class NormalDebugMode : MonoBehaviour
{
    UniversalRenderPipelineDebugDisplaySettings debugSettings = null;

    void Start()
    {
        debugSettings = UniversalRenderPipelineDebugDisplaySettings.Instance;

        if (debugSettings != null)
            debugSettings.materialSettings.materialDebugMode = DebugMaterialMode.NormalTangentSpace;

    }

    // Update is called once per frame
    void OnDestroy()
    {
        if (debugSettings != null)
            debugSettings.materialSettings.materialDebugMode = DebugMaterialMode.None;
    }
}
