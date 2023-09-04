using System.Collections.Generic;

namespace UnityEditor.VFX
{
    class VFXTopologyProvider : VariantProvider
    {
        protected sealed override Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "m_Topology", new object[] { new ParticleTopologyPlanarPrimitive(), new ParticleTopologyMesh() } }
                };
            }
        }
    }

    [VFXInfo(variantProvider = typeof(VFXTopologyProvider))]
    sealed class VFXComposedParticleOutput : VFXAbstractComposedParticleOutput
    {
        VFXComposedParticleOutput() : base(false) { }
    }
}
