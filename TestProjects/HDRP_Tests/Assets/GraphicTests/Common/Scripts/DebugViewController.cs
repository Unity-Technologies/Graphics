using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
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

    [ContextMenu("Set Debug View")]
    public void SetDebugView()
    {
        HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

        switch ( settingType )
        {
            case SettingType.Material:
                hdPipeline.debugDisplaySettings.SetDebugViewGBuffer(gBuffer);
                hdPipeline.debugDisplaySettings.fullScreenDebugMode = FullScreenDebugMode.None;
                break;
            case SettingType.Rendering:
                hdPipeline.debugDisplaySettings.SetDebugViewGBuffer(0);
                hdPipeline.debugDisplaySettings.fullScreenDebugMode = (FullScreenDebugMode) fullScreenDebugMode;
                break;
        }
    }

    void OnDestroy()
    {
        HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

        hdPipeline.debugDisplaySettings.SetDebugViewGBuffer(0);
        hdPipeline.debugDisplaySettings.fullScreenDebugMode = FullScreenDebugMode.None;
    }
}
