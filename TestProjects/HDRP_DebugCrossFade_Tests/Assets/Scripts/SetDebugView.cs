using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;
using UnityEngine.Rendering;

public class SetDebugView : MonoBehaviour
{
    [SerializeField] MyDebugData debugData;

    MyDebugData previousDebugData;

    [SerializeField, TextArea] string jsonData;
    [SerializeField] int[] debugViewMaterial;

    [SerializeField, TextArea] string jsonSettings;

    [ContextMenu("Get Current Debug Settings")]
    void GetCurrentDebugSettings ()
    {
        var hdrPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrPipeline == null) return;

        if (debugData == null) debugData = new MyDebugData();

        ConvertData( hdrPipeline.debugDisplaySettings.data, debugData );
        GetDataJson();
    }

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        var hdrPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

        Debug.Log(hdrPipeline);

        if (hdrPipeline != null)
        {
            if (previousDebugData == null) previousDebugData = new MyDebugData();

            ConvertData(hdrPipeline.debugDisplaySettings.data, previousDebugData);

            //ApplyData(debugData, hdrPipeline.debugDisplaySettings.data);
            SetDataJson();

            hdrPipeline.debugDisplaySettings.data.fullScreenDebugMode = debugData.fullScreenDebugMode;
        }
    }

    void GetDataJson()
    {
        var hdrPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrPipeline == null) return;

        jsonData = JsonUtility.ToJson(hdrPipeline.debugDisplaySettings.data);


        //debugViewMaterial = hdrPipeline.debugDisplaySettings.data.materialDebugSettings.debugViewMaterial;

        Debug.Log( debugViewMaterial.Aggregate( "debugViewMaterial: ", (s, i) => s += ", "+i.ToString() ) );

        jsonSettings = JsonUtility.ToJson(hdrPipeline.debugDisplaySettings);
    }

    void SetDataJson()
    {
        var hdrPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

        if ( jsonData == null || hdrPipeline == null) return;

        JsonUtility.FromJsonOverwrite(jsonData, hdrPipeline.debugDisplaySettings.data);

        hdrPipeline.debugDisplaySettings.UpdateMaterials();
        hdrPipeline.debugDisplaySettings.UpdateCameraFreezeOptions();

        // JsonUtility.FromJsonOverwrite(jsonSettings, hdrPipeline.debugDisplaySettings);

        hdrPipeline.debugDisplaySettings.data.materialDebugSettings.SetDebugViewCommonMaterialProperty( hdrPipeline.debugDisplaySettings.data.materialDebugSettings.debugViewMaterialCommonValue );
        hdrPipeline.debugDisplaySettings.data.materialDebugSettings.SetDebugViewMaterial( debugViewMaterial[1] );
        hdrPipeline.debugDisplaySettings.data.materialDebugSettings.SetDebugViewGBuffer( hdrPipeline.debugDisplaySettings.data.gBufferEnumIndex );

        Debug.Log(hdrPipeline.debugDisplaySettings.data.gBufferEnumIndex);

        Debug.Log(hdrPipeline.debugDisplaySettings.IsDebugDisplayEnabled());
    }

    void ConvertData(DebugDisplaySettings.DebugData source, MyDebugData target)
    {
        if (source == null || target == null) return;

        target.debugOverlayRatio = source.debugOverlayRatio;
        target.fullScreenDebugMode = source.fullScreenDebugMode;
        target.fullscreenDebugMip = source.fullscreenDebugMip;
        target.fullScreenContactShadowLightIndex = source.fullScreenContactShadowLightIndex;
        target.showSSSampledColor = source.showSSSampledColor;
        target.showContactShadowFade = source.showContactShadowFade;

        //target.materialDebugSettings = source.materialDebugSettings;
        target.materialDebugSettings.debugViewMaterialCommonValue = source.materialDebugSettings.debugViewMaterialCommonValue;
        target.materialDebugSettings.materialValidateLowColor = source.materialDebugSettings.materialValidateLowColor;
        target.materialDebugSettings.materialValidateHighColor = source.materialDebugSettings.materialValidateHighColor;
        target.materialDebugSettings.materialValidateTrueMetalColor = source.materialDebugSettings.materialValidateTrueMetalColor;
        target.materialDebugSettings.materialValidateTrueMetal = source.materialDebugSettings.materialValidateTrueMetal;

        target.lightingDebugSettings = source.lightingDebugSettings;
        target.mipMapDebugSettings = source.mipMapDebugSettings;
        target.colorPickerDebugSettings = source.colorPickerDebugSettings;
        target.falseColorDebugSettings = source.falseColorDebugSettings;
        target.decalsDebugSettings = source.decalsDebugSettings;
        target.msaaSamples = source.msaaSamples;

#if ENABLE_RAYTRACING
        target.countRays = source.countRays;
        target.showRaysPerFrame = source.showRaysPerFrame;
        target.raysPerFrameFontColor = source.raysPerFrameFontColor;
#endif

        target.debugCameraToFreeze = source.debugCameraToFreeze;

        target.lightingDebugModeEnumIndex = source.lightingDebugModeEnumIndex;
        target.lightingFulscreenDebugModeEnumIndex = source.lightingFulscreenDebugModeEnumIndex;
        target.tileClusterDebugEnumIndex = source.tileClusterDebugEnumIndex;
        target.mipMapsEnumIndex = source.mipMapsEnumIndex;
        target.engineEnumIndex = source.engineEnumIndex;
        target.attributesEnumIndex = source.attributesEnumIndex;
        target.propertiesEnumIndex = source.propertiesEnumIndex;
        target.gBufferEnumIndex = source.gBufferEnumIndex;
        target.shadowDebugModeEnumIndex = source.shadowDebugModeEnumIndex;
        target.tileClusterDebugByCategoryEnumIndex = source.tileClusterDebugByCategoryEnumIndex;
        target.lightVolumeDebugTypeEnumIndex = source.lightVolumeDebugTypeEnumIndex;
        target.renderingFulscreenDebugModeEnumIndex = source.renderingFulscreenDebugModeEnumIndex;
        target.terrainTextureEnumIndex = source.terrainTextureEnumIndex;
        target.colorPickerDebugModeEnumIndex = source.colorPickerDebugModeEnumIndex;
        target.msaaSampleDebugModeEnumIndex = source.msaaSampleDebugModeEnumIndex;
        target.debugCameraToFreezeEnumIndex = source.debugCameraToFreezeEnumIndex;
    }

    void ApplyData( MyDebugData source, DebugDisplaySettings.DebugData target )
    {
        if (source == null) return;

        target.debugOverlayRatio = source.debugOverlayRatio;
        target.fullScreenDebugMode = source.fullScreenDebugMode;
        target.fullscreenDebugMip = source.fullscreenDebugMip;
        target.fullScreenContactShadowLightIndex = source.fullScreenContactShadowLightIndex;
        target.showSSSampledColor = source.showSSSampledColor;
        target.showContactShadowFade = source.showContactShadowFade;

        //target.materialDebugSettings = source.materialDebugSettings;
        target.materialDebugSettings.debugViewMaterialCommonValue = source.materialDebugSettings.debugViewMaterialCommonValue;
        target.materialDebugSettings.materialValidateLowColor = source.materialDebugSettings.materialValidateLowColor;
        target.materialDebugSettings.materialValidateHighColor = source.materialDebugSettings.materialValidateHighColor;
        target.materialDebugSettings.materialValidateTrueMetalColor = source.materialDebugSettings.materialValidateTrueMetalColor;
        target.materialDebugSettings.materialValidateTrueMetal = source.materialDebugSettings.materialValidateTrueMetal;

        target.lightingDebugSettings = source.lightingDebugSettings;
        target.mipMapDebugSettings = source.mipMapDebugSettings;
        target.colorPickerDebugSettings = source.colorPickerDebugSettings;
        target.falseColorDebugSettings = source.falseColorDebugSettings;
        target.decalsDebugSettings = source.decalsDebugSettings;
        target.msaaSamples = source.msaaSamples;

#if ENABLE_RAYTRACING
        target.countRays = source.countRays;
        target.showRaysPerFrame = source.showRaysPerFrame;
        target.raysPerFrameFontColor = source.raysPerFrameFontColor;
#endif

        target.debugCameraToFreeze = source.debugCameraToFreeze;

        target.lightingDebugModeEnumIndex = source.lightingDebugModeEnumIndex;
        target.lightingFulscreenDebugModeEnumIndex = source.lightingFulscreenDebugModeEnumIndex;
        target.tileClusterDebugEnumIndex = source.tileClusterDebugEnumIndex;
        target.mipMapsEnumIndex = source.mipMapsEnumIndex;
        target.engineEnumIndex = source.engineEnumIndex;
        target.attributesEnumIndex = source.attributesEnumIndex;
        target.propertiesEnumIndex = source.propertiesEnumIndex;
        target.gBufferEnumIndex = source.gBufferEnumIndex;
        target.shadowDebugModeEnumIndex = source.shadowDebugModeEnumIndex;
        target.tileClusterDebugByCategoryEnumIndex = source.tileClusterDebugByCategoryEnumIndex;
        target.lightVolumeDebugTypeEnumIndex = source.lightVolumeDebugTypeEnumIndex;
        target.renderingFulscreenDebugModeEnumIndex = source.renderingFulscreenDebugModeEnumIndex;
        target.terrainTextureEnumIndex = source.terrainTextureEnumIndex;
        target.colorPickerDebugModeEnumIndex = source.colorPickerDebugModeEnumIndex;
        target.msaaSampleDebugModeEnumIndex = source.msaaSampleDebugModeEnumIndex;
        target.debugCameraToFreezeEnumIndex = source.debugCameraToFreezeEnumIndex;
    }

    // Copy of the DebugDisplaySettings.DebugData class, but that can be serialized
    [Serializable]
    public class MyDebugData
    {
        public float debugOverlayRatio = 0.33f;
        public FullScreenDebugMode fullScreenDebugMode = FullScreenDebugMode.None;
        public float fullscreenDebugMip = 0.0f;
        public int fullScreenContactShadowLightIndex = 0;
        public bool showSSSampledColor = false;
        public bool showContactShadowFade = false;

        // This one doesn't allow to serialize the class
        // public MaterialDebugSettings materialDebugSettings = new MaterialDebugSettings();
        public MyMaterialDebugSettings materialDebugSettings = new MyMaterialDebugSettings();

        public LightingDebugSettings lightingDebugSettings = new LightingDebugSettings();
        public MipMapDebugSettings mipMapDebugSettings = new MipMapDebugSettings();
        public ColorPickerDebugSettings colorPickerDebugSettings = new ColorPickerDebugSettings();
        public FalseColorDebugSettings falseColorDebugSettings = new FalseColorDebugSettings();
        public DecalsDebugSettings decalsDebugSettings = new DecalsDebugSettings();
        public MSAASamples msaaSamples = MSAASamples.None;

        // Raytracing
#if ENABLE_RAYTRACING
        public bool countRays = false;
        public bool showRaysPerFrame = false;
        public Color raysPerFrameFontColor = Color.white;
#endif

        public int debugCameraToFreeze = 0;

        //saved enum fields for when repainting
        public int lightingDebugModeEnumIndex;
        public int lightingFulscreenDebugModeEnumIndex;
        public int tileClusterDebugEnumIndex;
        public int mipMapsEnumIndex;
        public int engineEnumIndex;
        public int attributesEnumIndex;
        public int propertiesEnumIndex;
        public int gBufferEnumIndex;
        public int shadowDebugModeEnumIndex;
        public int tileClusterDebugByCategoryEnumIndex;
        public int lightVolumeDebugTypeEnumIndex;
        public int renderingFulscreenDebugModeEnumIndex;
        public int terrainTextureEnumIndex;
        public int colorPickerDebugModeEnumIndex;
        public int msaaSampleDebugModeEnumIndex;
        public int debugCameraToFreezeEnumIndex;
    }

    // Copy of the MaterialDebugSettings class, but that can be serialized
    [Serializable]
    public class MyMaterialDebugSettings
    {
        public MaterialSharedProperty debugViewMaterialCommonValue = MaterialSharedProperty.None;

        public Color materialValidateLowColor = new Color(1.0f, 0.0f, 0.0f);
        public Color materialValidateHighColor = new Color(0.0f, 0.0f, 1.0f);
        public Color materialValidateTrueMetalColor = new Color(1.0f, 1.0f, 0.0f);
        public bool  materialValidateTrueMetal = false;
    }
}
