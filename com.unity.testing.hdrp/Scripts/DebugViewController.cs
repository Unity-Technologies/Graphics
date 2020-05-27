using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

public class DebugViewController : MonoBehaviour
{
    public enum SettingType { Material, Rendering }
    public SettingType settingType = SettingType.Material;

    [Header("Material")]
    [SerializeField] int gBuffer = 0;

    //DebugItemHandlerIntEnum(MaterialDebugSettings.debugViewMaterialGBufferStrings, MaterialDebugSettings.debugViewMaterialGBufferValues)
    [Header("Rendering")]
    [SerializeField] int fullScreenDebugMode = 0;

    [SerializeField] bool lightlayers = false;

    [ContextMenu("Set Debug View")]
    public void SetDebugView()
    {
        HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

        switch ( settingType )
        {
            case SettingType.Material:
                hdPipeline.debugDisplaySettings.SetDebugViewGBuffer(gBuffer);
                break;
            case SettingType.Rendering:
                hdPipeline.debugDisplaySettings.SetFullScreenDebugMode((FullScreenDebugMode) fullScreenDebugMode);
                break;
        }

        if (lightlayers)
        {
            hdPipeline.debugDisplaySettings.SetDebugLightLayersMode(true);
            hdPipeline.debugDisplaySettings.data.lightingDebugSettings.debugLightLayersFilterMask = (DebugLightLayersMask)0b10111101;
        }
    }

    void OnDestroy()
    {
        HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdPipeline != null)
            ((IDebugData)hdPipeline.debugDisplaySettings).GetReset().Invoke();
    }
}
