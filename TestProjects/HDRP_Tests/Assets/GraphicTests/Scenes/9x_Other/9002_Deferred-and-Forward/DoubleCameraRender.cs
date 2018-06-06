using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class DoubleCameraRender : MonoBehaviour
{
    [Header("Forward Path")]
    [SerializeField] private Camera fwd_Camera;
    [SerializeField] private RenderPipelineAsset fwd_RenderPipelineAsset;

    [Header("Deferred Path")]
    [SerializeField] private Camera dfd_Camera;
    [SerializeField] private RenderPipelineAsset dfd_RenderPipelineAsset;

    [SerializeField] private bool refresh = false;


    [ContextMenu("Refresh targets")]
    public void RefreshTargets()
    {
        RenderPipelineAsset oldPipelineAsset = GraphicsSettings.renderPipelineAsset;

        GraphicsSettings.renderPipelineAsset = fwd_RenderPipelineAsset;
        fwd_Camera.enabled = true;
        fwd_Camera.Render();
        fwd_Camera.enabled = false;

        GraphicsSettings.renderPipelineAsset = dfd_RenderPipelineAsset;
        dfd_Camera.enabled = true;
        dfd_Camera.Render();
        dfd_Camera.enabled = false;

        GraphicsSettings.renderPipelineAsset = oldPipelineAsset;
    }

    private void OnValidate()
    {
        if (refresh)
        {
            RefreshTargets();
            refresh = false;
        }
    }
}
