using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SetupMipMapModeAndRender : MonoBehaviour
{
    public Camera attachedCam = null;

    public DebugMipInfoMode mipInfoMode = DebugMipInfoMode.None;
    public DebugMipMapModeTerrainTexture mipDebugTerrainTexture = DebugMipMapModeTerrainTexture.Control;
    public float mipDebugOpacity = 1.0f;
    public int mipDebugMaterialTextureSlot = 0;
    public DebugMipMapStatusMode mipDebugStatusMode = DebugMipMapStatusMode.Material;
    public bool mipDebugStatusShowCode = true;

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
        UniversalRenderPipelineDebugDisplaySettings debugSettings = UniversalRenderPipelineDebugDisplaySettings.Instance;
        if (debugSettings == null || debugSettings.renderingSettings == null)
            return;

        DebugMipInfoMode previousMipInfoMode = debugSettings.renderingSettings.mipInfoMode;
        DebugMipMapModeTerrainTexture previousTerrainTexture = debugSettings.renderingSettings.mipDebugTerrainTexture;
        float previousMipMapOpacity = debugSettings.renderingSettings.mipDebugOpacity;
        bool previousShowInfoForAllSlots = debugSettings.renderingSettings.showInfoForAllSlots;
        int previousMaterialTextureSlot = debugSettings.renderingSettings.mipDebugMaterialTextureSlot;
        DebugMipMapStatusMode previousStatusMode = debugSettings.renderingSettings.mipDebugStatusMode;
        bool previousShowCode = debugSettings.renderingSettings.mipDebugStatusShowCode;

        debugSettings.renderingSettings.mipInfoMode = mipInfoMode;
        debugSettings.renderingSettings.mipDebugTerrainTexture = mipDebugTerrainTexture;
        debugSettings.renderingSettings.mipDebugOpacity = mipDebugOpacity;
        debugSettings.renderingSettings.showInfoForAllSlots = mipDebugStatusMode == DebugMipMapStatusMode.Material;
        debugSettings.renderingSettings.mipDebugMaterialTextureSlot = mipDebugMaterialTextureSlot;
        debugSettings.renderingSettings.mipDebugStatusMode = mipDebugStatusMode;
        debugSettings.renderingSettings.mipDebugStatusShowCode = mipDebugStatusShowCode;

        if (attachedCam)
            attachedCam.Render();

        debugSettings.renderingSettings.mipInfoMode = previousMipInfoMode;
        debugSettings.renderingSettings.mipDebugTerrainTexture = previousTerrainTexture;
        debugSettings.renderingSettings.mipDebugOpacity = previousMipMapOpacity;
        debugSettings.renderingSettings.showInfoForAllSlots = previousShowInfoForAllSlots;
        debugSettings.renderingSettings.mipDebugMaterialTextureSlot = previousMaterialTextureSlot;
        debugSettings.renderingSettings.mipDebugStatusMode = previousStatusMode;
        debugSettings.renderingSettings.mipDebugStatusShowCode = previousShowCode;
    }
}
