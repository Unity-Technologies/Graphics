using UnityEngine;
using UnityEngine.Rendering;

public class OverrideHDRPAsset : MonoBehaviour
{
    [Tooltip("Specifies the render pipeline asset used when executing the test.")]
    public RenderPipelineAsset renderPipelineAsset;

    private RenderPipelineAsset backupPipelineAsset;

    void Awake()
    {
        backupPipelineAsset = GraphicsSettings.defaultRenderPipeline;
        GraphicsSettings.defaultRenderPipeline = renderPipelineAsset;
        Debug.Log($"Changed RP Asset from {backupPipelineAsset?.name} to : {renderPipelineAsset?.name}");
    }

    private void OnDestroy()
    {
        GraphicsSettings.defaultRenderPipeline = backupPipelineAsset;
        Debug.Log($"Restored RP Asset to {backupPipelineAsset?.name}");
    }
}

