using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

public class SpeedTreeTestSetup : MonoBehaviour
{
    void OnEnable()
    {
        if (GraphicsSettings.renderPipelineAsset != null)
        {
            var lwAsset = (LightweightRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;
            distance = lwAsset.shadowDistance;
            lwAsset.shadowDistance = 1000.0f;
        }
    }

    void OnDisable()
    {
        if (GraphicsSettings.renderPipelineAsset != null)
        {
            var lwAsset = (LightweightRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;
            lwAsset.shadowDistance = distance;
        }
    }

    float distance = 0.0f;
}
