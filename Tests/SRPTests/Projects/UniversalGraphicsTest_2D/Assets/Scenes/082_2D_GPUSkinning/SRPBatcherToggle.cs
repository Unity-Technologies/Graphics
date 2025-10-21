using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class SRPBatcherToggle : MonoBehaviour
{
    public bool useSRPBatcher = false;
    private bool useScriptableRenderPipelineBatching;
    // called second
    void OnEnable()
    {
        var universalAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        Debug.LogFormat("OnEnable {0}", universalAsset.useSRPBatcher);
        useScriptableRenderPipelineBatching = universalAsset.useSRPBatcher;
        universalAsset.useSRPBatcher = useSRPBatcher;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
    }

    private void Update()
    {

    }

    // called when the game is terminated
    void OnDisable()
    {
        var universalAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        universalAsset.useSRPBatcher = useScriptableRenderPipelineBatching;
        GraphicsSettings.useScriptableRenderPipelineBatching = useScriptableRenderPipelineBatching;
        Debug.LogFormat("OnDisable {0}", universalAsset.useSRPBatcher);
    }
}
