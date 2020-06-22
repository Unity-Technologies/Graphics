using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{

    [GenerateHLSL]
    enum CapsuleOcclusionType
    {
        None,
        AmbientOcclusion = (1 << 0),
        SpecularOcclusion = (1 << 1),
        DirectionalShadows = (1 << 2)
    }

    class CapsuleOcclusionManager
    {
        const int k_LUTWidth = 128;
        const int k_LUTHeight = 64;

        RTHandle m_CapsuleSoftShadowLUT;

        internal void AllocRTs()
        {
            // Enough precision?
            m_CapsuleSoftShadowLUT = RTHandles.Alloc(k_LUTWidth, k_LUTHeight, colorFormat: GraphicsFormat.R8_UNorm,
                                            enableRandomWrite: true, 
                                            name: "Capsule Soft Shadows LUT");
        }

        internal void Cleanup()
        {
            RTHandles.Release(m_CapsuleSoftShadowLUT);
        }

        internal void GenerateCapsuleSoftShadowsLUT(float coneAngle)
        {
            // TODO
        }
    }
}
