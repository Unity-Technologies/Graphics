using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Setup a specific render pipeline on scene loading.
/// </summary>
[Obsolete("This component have been deprecated due to it using bad practice (changing static data in OnValidate) #from(2022.2)", false)]
[ExecuteAlways]
public class SceneRenderPipeline : MonoBehaviour
{
    /// <summary>
    /// Scriptable Render Pipeline Asset to setup on scene load.
    /// </summary>
    public RenderPipelineAsset renderPipelineAsset = GraphicsSettings.renderPipelineAsset;

    void OnEnable()
    {
        GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
    }

    void OnValidate()
    {
        GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
    }
}
