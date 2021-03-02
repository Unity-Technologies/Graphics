// This file should be used as a container for things on its
// way to being deprecated and removed in future releases

using System;

namespace UnityEngine.Rendering.Universal
{
    public static partial class ShaderInput
    {
        //Even when RenderingUtils.useStructuredBuffer is true we do not this structure anymore, because in shader side worldToShadowMatrix and shadowParams must be stored in arrays of different sizes
        // To specify shader-side shadow matrices and shadow parameters, see code in AdditionalLightsShadowCasterPass.SetupAdditionalLightsShadowReceiverConstants
        [Obsolete("ShaderInput.ShadowData was deprecated. Shadow slice matrices and per-light shadow parameters are now passed to the GPU using entries in buffers m_AdditionalLightsWorldToShadow_SSBO and m_AdditionalShadowParams_SSBO", false)]
        public struct ShadowData
        {
            public Matrix4x4 worldToShadowMatrix;
            public Vector4 shadowParams;
        }
    }
}
