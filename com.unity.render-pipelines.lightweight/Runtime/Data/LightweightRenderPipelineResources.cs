using UnityEngine.Serialization;

namespace UnityEngine.Rendering.LWRP
{
    public class LightweightRenderPipelineResources : ScriptableObject
    {
        [FormerlySerializedAs("BlitShader"), SerializeField] Shader m_BlitShader = null;
        [FormerlySerializedAs("CopyDepthShader"), SerializeField] Shader m_CopyDepthShader = null;
        [FormerlySerializedAs("ScreenSpaceShadowShader"), SerializeField] Shader m_ScreenSpaceShadowShader = null;
        [FormerlySerializedAs("ScreenSpaceShadowCompute"), SerializeField] ComputeShader m_ScreenSpaceShadowComputeShader = null; //seongdae;vxsm
        [FormerlySerializedAs("SamplingShader"), SerializeField] Shader m_SamplingShader = null;

        public Shader blitShader
        {
            get { return m_BlitShader; }
        }

        public Shader copyDepthShader
        {
            get { return m_CopyDepthShader; }
        }

        public Shader screenSpaceShadowShader
        {
            get { return m_ScreenSpaceShadowShader; }
        }

        //seongdae;vxsm
        public ComputeShader screenSpaceShadowComputeShader
        {
            get { return m_ScreenSpaceShadowComputeShader; }
        }
        //seongdae;vxsm

        public Shader samplingShader
        {
            get { return m_SamplingShader; }
        }
    }
}
