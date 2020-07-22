using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace kTools.Motion
{
    sealed class MotionBlurRenderPass : ScriptableRenderPass
    {
#region Fields
        const string kMotionBlurShader = "Hidden/kMotion/MotionBlur";
        const string kProfilingTag = "Motion Blur";

        static readonly string[] s_ShaderTags = new string[]
        {
            "UniversalForward",
            "LightweightForward",
        };

        Material m_Material;
        MotionBlur m_MotionBlur;
#endregion

#region Constructors
        public MotionBlurRenderPass()
        {
            // Set data
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }
#endregion

#region Setup
        internal void Setup(MotionBlur motionBlur)
        {
            // Set data
            ConfigureInput(ScriptableRenderPassInput.Motion);
            m_MotionBlur = motionBlur;
            m_Material = new Material(Shader.Find(kMotionBlurShader));
        }
#endregion

#region Execution
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(kProfilingTag);

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Get data
            var camera = renderingData.cameraData.camera;

            // Never draw in Preview
            if(camera.cameraType == CameraType.Preview)
                return;

            // Profiling command
            
            CommandBuffer cmd = CommandBufferPool.Get(kProfilingTag);
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                // Set Material properties from VolumeComponent
                m_Material.SetFloat("_Intensity", m_MotionBlur.intensity.value);

                // TODO: Why doesnt RenderTargetHandle.CameraTarget work?
                var colorTextureIdentifier = new RenderTargetIdentifier("_CameraColorTexture");

                // RenderTexture
                var descriptor = new RenderTextureDescriptor(camera.scaledPixelWidth, camera.scaledPixelHeight, RenderTextureFormat.DefaultHDR, 16);
                var renderTexture = RenderTexture.GetTemporary(descriptor);

                // Blits
                var passIndex = (int)m_MotionBlur.quality.value;
                cmd.Blit(colorTextureIdentifier, renderTexture, m_Material, passIndex);
                cmd.Blit(renderTexture, colorTextureIdentifier, m_Material, passIndex);
                ExecuteCommand(context, cmd);

                RenderTexture.ReleaseTemporary(renderTexture);
            }
            ExecuteCommand(context, cmd);
        }
#endregion

#region CommandBufer
        void ExecuteCommand(ScriptableRenderContext context, CommandBuffer cmd)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
#endregion
    }
}