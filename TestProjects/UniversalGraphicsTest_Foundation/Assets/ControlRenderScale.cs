using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class ControlRenderScale : MonoBehaviour
{
    public float playRenderScale = 0.25f;
    float originalScale = 1.0f;

    // Update is called once per frame
    void Update()
    {
        // this doesn't seem to do anything...
        // ScalableBufferManager.ResizeBuffers(0.25f, 0.25f);
    }

    private void OnEnable()
    {
        // UniversalRenderPipelineAsset asset = new UniversalRenderPipelineAsset();
        var curAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        if (curAsset != null)
        {
            originalScale = curAsset.renderScale;
            curAsset.renderScale = playRenderScale;
            GraphicsSettings.renderPipelineAsset = curAsset;
        }
    }

    private void OnDisable()
    {
        var curAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        if (curAsset != null)
        {
            curAsset.renderScale = originalScale;
            GraphicsSettings.renderPipelineAsset = curAsset;
        }
    }
}
