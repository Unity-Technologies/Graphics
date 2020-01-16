using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Setup a specific render pipeline on scene loading.
/// </summary>
[ExecuteAlways]
public class SceneRenderPipeline : MonoBehaviour
{
    /// <summary>
    /// Scriptable Render Pipeline Asset to setup on scene load.
    /// </summary>
    public RenderPipelineAsset renderPipelineAsset;

    void OnEnable()
    {
        GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
    }

    void OnValidate()
    {
        GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
    }
}
