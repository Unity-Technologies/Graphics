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
    [SerializeField, TextArea] string jsonData;
    [SerializeField] int[] debugViewMaterial;
    [SerializeField] DebugViewVarying debugViewVarying;
    [SerializeField] DebugViewProperties debugViewProperties;

    [ContextMenu("Get Current Debug Settings")]
    void GetCurrentDebugSettings ()
    {
        var hdrPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrPipeline == null) return;
        GetDataJson();
    }

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        var hdrPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

        Debug.Log(hdrPipeline);

        if (hdrPipeline != null)
        {
            SetDataJson();
        }
    }

    void GetDataJson()
    {
        var hdrPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrPipeline == null) return;

        jsonData = JsonUtility.ToJson(hdrPipeline.debugDisplaySettings.data);

        debugViewMaterial = hdrPipeline.debugDisplaySettings.data.materialDebugSettings.debugViewMaterial;
        debugViewVarying = hdrPipeline.debugDisplaySettings.data.materialDebugSettings.debugViewVarying;
        debugViewProperties = hdrPipeline.debugDisplaySettings.data.materialDebugSettings.debugViewProperties;

        //Debug.Log( debugViewMaterial.Aggregate( "debugViewMaterial: ", (s, i) => s += ", "+i.ToString() ) );
    }

    void SetDataJson()
    {
        var hdrPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

        if ( jsonData == null || hdrPipeline == null) return;

        JsonUtility.FromJsonOverwrite(jsonData, hdrPipeline.debugDisplaySettings.data);

        hdrPipeline.debugDisplaySettings.UpdateMaterials();
        hdrPipeline.debugDisplaySettings.UpdateCameraFreezeOptions();

        hdrPipeline.debugDisplaySettings.SetDebugViewCommonMaterialProperty( hdrPipeline.debugDisplaySettings.data.materialDebugSettings.debugViewMaterialCommonValue );
        hdrPipeline.debugDisplaySettings.SetDebugViewMaterial( debugViewMaterial[1] );
        hdrPipeline.debugDisplaySettings.SetDebugViewEngine( hdrPipeline.debugDisplaySettings.data.engineEnumIndex );
        hdrPipeline.debugDisplaySettings.SetDebugViewVarying( hdrPipeline.debugDisplaySettings.data.materialDebugSettings.debugViewVarying );
        hdrPipeline.debugDisplaySettings.SetDebugViewProperties( hdrPipeline.debugDisplaySettings.data.materialDebugSettings.debugViewProperties );
        hdrPipeline.debugDisplaySettings.SetDebugViewGBuffer( hdrPipeline.debugDisplaySettings.data.gBufferEnumIndex );
        hdrPipeline.debugDisplaySettings.SetFullScreenDebugMode( hdrPipeline.debugDisplaySettings.data.fullScreenDebugMode );
        //hdrPipeline.debugDisplaySettings.SetShadowDebugMode( hdrPipeline.debugDisplaySettings.data. );
        //hdrPipeline.debugDisplaySettings.SetDebugLightFilterMode( hdrPipeline.debugDisplaySettings.data. );
        hdrPipeline.debugDisplaySettings.SetDebugLightingMode( (DebugLightingMode) hdrPipeline.debugDisplaySettings.data.lightingDebugModeEnumIndex );
        hdrPipeline.debugDisplaySettings.SetMipMapMode( (DebugMipMapMode) hdrPipeline.debugDisplaySettings.data.mipMapsEnumIndex );
        
        hdrPipeline.debugDisplaySettings.UpdateMaterials();
        hdrPipeline.debugDisplaySettings.UpdateCameraFreezeOptions();

        // Debug.Log(hdrPipeline.debugDisplaySettings.data.gBufferEnumIndex);

        // Debug.Log(hdrPipeline.debugDisplaySettings.IsDebugDisplayEnabled());
    }
}
