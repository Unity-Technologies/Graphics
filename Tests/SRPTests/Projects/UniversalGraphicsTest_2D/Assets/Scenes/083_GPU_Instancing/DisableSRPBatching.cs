using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class DisableSRPBatching : MonoBehaviour
{
    private void Update()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = false;
    }
}
