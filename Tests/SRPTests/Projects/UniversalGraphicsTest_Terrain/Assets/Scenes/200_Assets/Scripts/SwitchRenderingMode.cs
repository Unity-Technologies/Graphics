using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SwitchRenderingMode : MonoBehaviour
{
    public UniversalRendererData rendererData;
    public RenderingMode renderingMode;

    private RenderingMode prevRenderingMode;

    void Awake()
    {
        prevRenderingMode = rendererData.renderingMode;
        rendererData.renderingMode = renderingMode;
    }

    void OnDisable()
    {
        rendererData.renderingMode = prevRenderingMode;
    }
}
