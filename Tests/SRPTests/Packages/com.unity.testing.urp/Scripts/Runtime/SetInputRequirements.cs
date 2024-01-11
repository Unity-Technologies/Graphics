using System;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// This Feature does nothing but
/// * Set the RenderPassEvent
/// * Create a dummy ScriptableRenderPass
/// * Call the ConfigureInput() with the requirements set in the feature
/// This allows us to get depth prepasses, etc.
/// </summary>
public class SetInputRequirements : ScriptableRendererFeature
{
    public ScriptableRenderPassInput inputRequirement;
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
    public int renderPassEventAdjustment = 0;
    private DummyPass m_DummyPass;


    /// <inheritdoc/>
    public override void Create()
    {
        m_DummyPass = new DummyPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_DummyPass.renderPassEvent = renderPassEvent + renderPassEventAdjustment;
        m_DummyPass.Setup( inputRequirement);
        renderer.EnqueuePass(m_DummyPass);
    }

    class DummyPass : ScriptableRenderPass
    {
        private ProfilingSampler m_ProfilingSampler;
        private PassData m_PassData;

        public DummyPass()
        {
            m_PassData = new PassData();
        }

        public void Setup(ScriptableRenderPassInput inputRequirement)
        {
            ConfigureInput(inputRequirement);
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        internal class PassData
        {
        }

        public static void ExecutePass(PassData passData, ScriptableRenderContext context, CommandBuffer cmd, bool yFlip)
        {
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }
}
