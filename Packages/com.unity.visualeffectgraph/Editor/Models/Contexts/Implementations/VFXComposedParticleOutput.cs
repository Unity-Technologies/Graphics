using System.Collections.Generic;

namespace UnityEditor.VFX
{
    class VFXTopologySubVariantProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            var primitives = new[] { VFXPrimitiveType.Octagon, VFXPrimitiveType.Triangle };
            foreach (var primitive in primitives)
            {
                var topology = VFXPlanarPrimitiveHelper.GetShaderDefine(primitive);
                yield return new Variant(
                    "Output Particle".AppendLabel("ShaderGraph").AppendLabel(primitive.ToString()),
                    null,
                    typeof(VFXComposedParticleOutput),
                    new[] { new KeyValuePair<string, object>("m_Topology", topology) });
            }
        }
    }

    class VFXTopologyProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                "Output Particle".AppendLabel("ShaderGraph").AppendLabel("Quad"),
                VFXLibraryStringHelper.Separator("Output Basic", 2),
                typeof(VFXComposedParticleOutput),
                new[] { new KeyValuePair<string, object>("m_Topology", new ParticleTopologyPlanarPrimitive()) },
                () => new VFXTopologySubVariantProvider());

            yield return new Variant(
                "Output Particle".AppendLabel("ShaderGraph").AppendLabel("Mesh"),
                VFXLibraryStringHelper.Separator("Output Basic", 2),
                typeof(VFXComposedParticleOutput),
                new[] { new KeyValuePair<string, object>("m_Topology", new ParticleTopologyMesh()) });
        }
    }

    [VFXInfo(variantProvider = typeof(VFXTopologyProvider), synonyms = new []{ "Shape" })]
    sealed class VFXComposedParticleOutput : VFXAbstractComposedParticleOutput
    {
        VFXComposedParticleOutput() : base(false) { }
    }
}
