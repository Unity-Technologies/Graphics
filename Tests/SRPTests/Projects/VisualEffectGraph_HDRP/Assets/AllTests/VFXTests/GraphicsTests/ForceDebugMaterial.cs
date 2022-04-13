using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class ForceDebugMaterial : MonoBehaviour
{
    [SerializeField]
    public UnityEngine.Rendering.HighDefinition.Attributes.MaterialSharedProperty debugProperty = UnityEngine.Rendering.HighDefinition.Attributes.MaterialSharedProperty.Albedo;

    void OnEnable()
    {
    }

    void Update()
    {
        var hdInstance = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdInstance != null)
        {
            var debugDisplaySettings = hdInstance.debugDisplaySettings;
            if (debugDisplaySettings != null)
            {
                debugDisplaySettings.SetDebugViewCommonMaterialProperty(debugProperty);
            }
        }
    }

    void OnDisable()
    {
        var hdInstance = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdInstance != null)
        {
            var debugDisplaySettings = hdInstance.debugDisplaySettings;
            if (debugDisplaySettings != null)
            {
                debugDisplaySettings.SetDebugViewCommonMaterialProperty(UnityEngine.Rendering.HighDefinition.Attributes.MaterialSharedProperty.None);
            }
        }
    }
}
