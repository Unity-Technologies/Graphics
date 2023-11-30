using System.Collections.Generic;

namespace UnityEditor.VFX
{
    class VFXTopologyProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                "Output Particle ShaderGraph Quad",
                "Output",
                typeof(VFXComposedParticleOutput),
                new[] { new KeyValuePair<string, object>("m_Topology", new ParticleTopologyPlanarPrimitive()) });

            yield return new Variant(
                "Output Particle ShaderGraph Mesh",
                "Output",
                typeof(VFXComposedParticleOutput),
                new[] { new KeyValuePair<string, object>("m_Topology", new ParticleTopologyMesh()) });
        }
    }

    [VFXInfo(variantProvider = typeof(VFXTopologyProvider))]
    sealed class VFXComposedParticleOutput : VFXAbstractComposedParticleOutput
    {
        VFXComposedParticleOutput() : base(false) { }
    }
}
