using System.Collections.Generic;

namespace UnityEditor.VFX
{
    class VFXStripTopologyProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                "Output Particle ShaderGraph Quad Strip",
                "Output",
                typeof(VFXComposedParticleOutput),
                new[] { new KeyValuePair<string, object>("m_Topology", new ParticleTopologyQuadStrip()) });
        }
    }

    [VFXInfo(variantProvider = typeof(VFXStripTopologyProvider))]
    sealed class VFXComposedParticleStripOutput : VFXAbstractComposedParticleOutput
    {
        VFXComposedParticleStripOutput() : base(true) { }
    }
}
