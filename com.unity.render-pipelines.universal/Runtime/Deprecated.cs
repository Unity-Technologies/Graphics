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

#pragma warning disable 618 // Obsolete warning
        [SerializeField] ShadowCascadesOption m_ShadowCascades = ShadowCascadesOption.NoCascades;

        [Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
        public ShadowCascadesOption shadowCascadeOption
        {
            get
            {
                return shadowCascadeCount switch
                {
                    1 => ShadowCascadesOption.NoCascades,
                    2 => ShadowCascadesOption.TwoCascades,
                    4 => ShadowCascadesOption.FourCascades,
                    _ => throw new InvalidOperationException("Cascade count is not compatible with obsolete API, please use shadowCascadeCount instead.")
                };
            }
            set
            {
                switch (value)
                {
                    case ShadowCascadesOption.NoCascades:
                        shadowCascadeCount = 1;
                        break;
                    case ShadowCascadesOption.TwoCascades:
                        shadowCascadeCount = 2;
                        break;
                    case ShadowCascadesOption.FourCascades:
                        shadowCascadeCount = 4;
                        break;
                    default:
                        throw new InvalidOperationException("Cascade count is not compatible with obsolete API, please use shadowCascadeCount instead.");
                }
            }
        }
#pragma warning restore 618 // Obsolete warning
    }
}
