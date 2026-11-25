using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class FixedTimeParamsUploader : MonoBehaviour
{
    public float desiredTime;

    readonly FixedTimePass m_Pass = new FixedTimePass();

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += EnqueuePass;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= EnqueuePass;
    }

    void EnqueuePass(ScriptableRenderContext context, Camera camera)
    {
        m_Pass.time = desiredTime;
        camera.GetUniversalAdditionalCameraData().scriptableRenderer?.EnqueuePass(m_Pass);
    }

    class FixedTimePass : ScriptableRenderPass
    {
        const float k_FixedDeltaTime = 1.0f / 30.0f;

        internal float time;

        public FixedTimePass()
        {
            renderPassEvent = RenderPassEvent.BeforeRendering;
        }

        // Recorded before EndFill, so this time is the camera's.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var vars = frameData.Get<UniversalCameraData>().renderer.m_GlobalShaderVariablesUploader;
            float lastTime = time - k_FixedDeltaTime;
            vars._TimeParameters = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);
            vars._LastTimeParameters = new Vector4(lastTime, Mathf.Sin(lastTime), Mathf.Cos(lastTime), 0.0f);
        }
    }
}
