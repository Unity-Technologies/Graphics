using System;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    class VFXStripTopologyProvider : VariantProvider
    {
        protected sealed override Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "m_Topology", new object[] { new ParticleTopologyQuadStrip() } }
                };
            }
        }
    }

    [VFXInfo(variantProvider = typeof(VFXStripTopologyProvider))]
    sealed class VFXComposedParticleStripOutput : VFXAbstractComposedParticleOutput
    {
        VFXComposedParticleStripOutput() : base(true) { }
    }
}
