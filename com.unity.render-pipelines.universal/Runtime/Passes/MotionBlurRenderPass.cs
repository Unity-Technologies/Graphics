using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace kTools.Motion
{
    sealed class MotionBlurRenderPass : ScriptableRenderPass
    {
        const string kProfilingTag = "Motion Blur";

        static readonly string[] s_ShaderTags = new string[]
        {
            "UniversalForward",
            "LightweightForward",
        };

        Material m_Material;
        MotionBlur m_MotionBlur;

        public MotionBlurRenderPass()
        {
            // Set data
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        }

        internal void Setup(MotionBlur motionBlur)
        {
            // Set data
            if(motionBlur.mode == MotionBlurMode.CameraAndObjects)
                ConfigureInput(ScriptableRenderPassInput.Motion);
            else
                ConfigureInput(ScriptableRenderPassInput.Depth);
            
            m_MotionBlur = motionBlur;
            m_Material = new Material(Shader.Find("Hidden/Universal Render Pipeline/CameraMotionBlur"));
        }
        
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
                var material = m_Material;

                var data = MotionVectorRendering.instance.GetMotionDataForCamera(camera);
            
                material.SetMatrix("_ViewProjM", data.viewProjectionMatrix);
                material.SetMatrix("_PrevViewProjM", data.previousViewProjectionMatrix);

                material.SetFloat("_Intensity", m_MotionBlur.intensity.value);
                material.SetFloat("_Clamp", m_MotionBlur.clamp.value);
            
                if(m_MotionBlur.mode == MotionBlurMode.CameraOnly)
                    material.EnableKeyword("_CAMERA_ONLY");
                else
                    material.DisableKeyword("_CAMERA_ONLY");
          
                // RenderTexture
                var descriptor = new RenderTextureDescriptor(camera.scaledPixelWidth, camera.scaledPixelHeight, RenderTextureFormat.DefaultHDR, 16);
                var renderTexture = RenderTexture.GetTemporary(descriptor);
                var colorTextureIdentifier = new RenderTargetIdentifier("_CameraColorTexture");

                // Blits
                var passIndex = (int)m_MotionBlur.quality.value;
                RenderingUtils.Blit(cmd, colorTextureIdentifier, renderTexture, m_Material, passIndex);
                cmd.Blit(renderTexture, colorTextureIdentifier);
                ExecuteCommand(context, cmd);

                RenderTexture.ReleaseTemporary(renderTexture);
            }
            ExecuteCommand(context, cmd);
        }
        
        void ExecuteCommand(ScriptableRenderContext context, CommandBuffer cmd)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}