// This file should be used as a container for things on its
// way to being deprecated and removed in future releases

using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    public abstract partial class ScriptableRenderPass
    {
        // This callback method will be removed. Please use OnCameraCleanup() instead.
        public virtual void FrameCleanup(CommandBuffer cmd) => OnCameraCleanup(cmd);
    }

    public partial class UniversalRenderPipelineAsset
    {
        [Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
        [MovedFrom("UnityEngine.Rendering.LWRP")] public enum ShadowCascadesOption
        {
            NoCascades,
            TwoCascades,
            FourCascades,
        }

        [Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
        [SerializeField] ShadowCascadesOption m_ShadowCascades = ShadowCascadesOption.NoCascades;

        [Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
        public ShadowCascadesOption shadowCascadeOption
        {
            get { return m_ShadowCascades; }
            set { m_ShadowCascades = value; }
        }
    }
}
