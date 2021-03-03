using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// inspired from https://github.com/Unity-Technologies/UniversalRenderingExamples/blob/master/Assets/Scripts/Runtime/AutoLoadPipelineAsset.cs
[ExecuteAlways]
public class SwapPipelineAsset : MonoBehaviour
{
    public UniversalRenderPipelineAsset defaultPipeline;
    public UniversalRenderPipelineAsset thisTestPipeline;

    private void OnEnable()
    {
        if (thisTestPipeline)
            GraphicsSettings.renderPipelineAsset = thisTestPipeline;
    }

    void OnDisable()
    {
        if (defaultPipeline)
            GraphicsSettings.renderPipelineAsset = defaultPipeline;
    }
}
