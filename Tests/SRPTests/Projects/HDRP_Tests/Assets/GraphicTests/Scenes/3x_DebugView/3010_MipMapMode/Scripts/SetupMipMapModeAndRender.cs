using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class SetupMipMapModeAndRender : MonoBehaviour
{
    public Camera attachedCam = null;

    public DebugMipMapMode mipMapMode = DebugMipMapMode.None;
    public DebugMipMapModeTerrainTexture mipMapModeTerrainTexture = DebugMipMapModeTerrainTexture.Control;
    public float mipMapOpacity = 1.0f;
    public int mipMapModeMaterialTextureSlot = 0;
    public DebugMipMapStatusMode mipMapStatusMode = DebugMipMapStatusMode.Material;
    public bool mipMapShowStatusCode = true;

    [Conditional("UNITY_ENABLE_CHECKS"), Conditional("UNITY_EDITOR")]
    void Start()
    {
        const int sceneRequiredStreamingTexMem = 8; // 8 MB -- a littler higher than really needed for our test scene, just to be sure.
        int nonStreamingTexMemInMegabytes = (int)(Texture.nonStreamingTextureMemory / (1024 * 1024));
        QualitySettings.streamingMipmapsMemoryBudget = nonStreamingTexMemInMegabytes + sceneRequiredStreamingTexMem;
        UnityEngine.Debug.Log($"'Texture.nonStreamingTextureMemory' is currently at {nonStreamingTexMemInMegabytes} MBs. 'QualitySettings.streamingMipmapsMemoryBudget' has been set to {QualitySettings.streamingMipmapsMemoryBudget} MBs.");
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    void Update()
    {
        HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdPipeline == null)
            return;

        DebugMipMapMode previousMipMapMode = hdPipeline.debugDisplaySettings.GetDebugMipMapMode();
        DebugMipMapModeTerrainTexture previousTerrainTexture = hdPipeline.debugDisplaySettings.GetDebugMipMapModeTerrainTexture();
        float previousMipMapOpacity = hdPipeline.debugDisplaySettings.GetDebugMipMapOpacity();
        bool previousShowInfoForAllSlots = hdPipeline.debugDisplaySettings.data.mipMapDebugSettings.showInfoForAllSlots;
        int previousMaterialTextureSlot = hdPipeline.debugDisplaySettings.GetDebugMipMapMaterialTextureSlot();
        DebugMipMapStatusMode previousStatusMode = hdPipeline.debugDisplaySettings.GetDebugMipMapStatusMode();
        bool previousShowStatusCode = hdPipeline.debugDisplaySettings.GetDebugMipMapShowStatusCode();

        hdPipeline.debugDisplaySettings.SetMipMapMode(mipMapMode);
        hdPipeline.debugDisplaySettings.SetDebugMipMapModeTerrainTexture(mipMapModeTerrainTexture);
        hdPipeline.debugDisplaySettings.SetDebugMipMapOpacity(mipMapOpacity);
        hdPipeline.debugDisplaySettings.data.mipMapDebugSettings.showInfoForAllSlots = mipMapStatusMode == DebugMipMapStatusMode.Material;
        hdPipeline.debugDisplaySettings.SetDebugMipMapMaterialTextureSlot(mipMapModeMaterialTextureSlot);
        hdPipeline.debugDisplaySettings.SetDebugMipMapStatusMode(mipMapStatusMode);
        hdPipeline.debugDisplaySettings.SetDebugMipMapShowStatusCode(mipMapShowStatusCode);

        if (attachedCam)
            attachedCam.Render();

        hdPipeline.debugDisplaySettings.SetMipMapMode(previousMipMapMode);
        hdPipeline.debugDisplaySettings.SetDebugMipMapModeTerrainTexture(previousTerrainTexture);
        hdPipeline.debugDisplaySettings.SetDebugMipMapOpacity(previousMipMapOpacity);
        hdPipeline.debugDisplaySettings.data.mipMapDebugSettings.showInfoForAllSlots = previousShowInfoForAllSlots;
        hdPipeline.debugDisplaySettings.SetDebugMipMapMaterialTextureSlot(previousMaterialTextureSlot);
        hdPipeline.debugDisplaySettings.SetDebugMipMapStatusMode(previousStatusMode);
        hdPipeline.debugDisplaySettings.SetDebugMipMapShowStatusCode(previousShowStatusCode);
    }
}
