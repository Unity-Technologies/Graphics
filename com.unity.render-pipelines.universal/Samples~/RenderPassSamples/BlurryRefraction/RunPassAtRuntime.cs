using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RunPassAtRuntime : MonoBehaviour
{
    public KawaseBlurSettings setting;
    KawaseBlur renderPass;

    bool useRenderPass = false;

    Material refactiveGlass;

    void Start()
    {
        refactiveGlass = GetComponent<Renderer>().material;
        renderPass = new KawaseBlur("KawaseBlur", setting);
    }

    void Update()
    {
        renderPass.renderPassEvent = setting.renderPassEvent;
        if(Input.GetKeyDown(KeyCode.C))
        {
            useRenderPass = !useRenderPass;
            refactiveGlass.SetFloat("IsUsingRenderFeature", useRenderPass ? 1.0f : 0.0f);

        }
        if(useRenderPass)
        {
            ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).scriptableRenderer.EnqueuePass(renderPass);
        }
    }
}
