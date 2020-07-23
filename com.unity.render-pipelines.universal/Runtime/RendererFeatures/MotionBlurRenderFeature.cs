using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace kTools.Motion
{
    sealed class MotionBlurRenderFeature : ScriptableRendererFeature
    {
        private  MotionBlurRenderPass m_MotionBlurRenderPass;

        #region Initialization
        public override void Create()
        {
            name = "Motion";
            if (m_MotionBlurRenderPass == null)
            {
                m_MotionBlurRenderPass = new MotionBlurRenderPass();
            }
        }
#endregion
        
#region RenderPass
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Motion blur pass
            var stack = VolumeManager.instance.stack;
            var motionBlur = stack.GetComponent<MotionBlur>();
            if (motionBlur.IsActive() && !renderingData.cameraData.isSceneViewCamera)
            {
                m_MotionBlurRenderPass.Setup(motionBlur);
                renderer.EnqueuePass(m_MotionBlurRenderPass);
            }
        }
#endregion
    }
}
