using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class _2DRenderer : ScriptableRenderer
    {
        Render2DLightingPass m_Render2DLightingPass;
        
        public _2DRenderer(_2DRendererData data) : base(data)
        {
            m_Render2DLightingPass = new Render2DLightingPass(data);
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            EnqueuePass(m_Render2DLightingPass);
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            cullingParameters.cullingOptions = CullingOptions.None;
            cullingParameters.isOrthographic = cameraData.camera.orthographic;
            cullingParameters.shadowDistance = 0.0f;
        }
    }
}
