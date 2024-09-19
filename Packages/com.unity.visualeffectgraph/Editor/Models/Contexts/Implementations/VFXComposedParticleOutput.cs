using System;
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
                yield return new VFXComposedParticleOutputVariant(
                    "Output Particle".AppendLabel("ShaderGraph").AppendLabel(primitive.ToString()),
                    null,
                    typeof(VFXComposedParticleOutput),
                    () => new ParticleTopologyPlanarPrimitive(primitive));
            }
        }
    }

    sealed class VFXComposedParticleOutputVariant : Variant
    {
        public VFXComposedParticleOutputVariant(string name, string category, Type modelType, Func<ParticleTrait> createTopology, Func<VariantProvider> variantProvider = null, string[] synonyms = null, bool supportFavorite = true)
            : base(name, category, modelType, null, variantProvider, synonyms, supportFavorite)
        {
            this.createTopology = createTopology;
        }

        public override VFXModel CreateInstance()
        {
            var newInstance = base.CreateInstance();
            newInstance.SetSettingValue("m_Topology", createTopology());
            return newInstance;
        }

        private readonly Func<ParticleTrait> createTopology;
    }

    class VFXTopologyProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new VFXComposedParticleOutputVariant(
                "Output Particle".AppendLabel("ShaderGraph").AppendLabel("Quad"),
                VFXLibraryStringHelper.Separator("Output Basic", 2),
                typeof(VFXComposedParticleOutput),
                () => new ParticleTopologyPlanarPrimitive(VFXPrimitiveType.Quad),
                () => new VFXTopologySubVariantProvider());

            yield return new VFXComposedParticleOutputVariant(
                "Output Particle".AppendLabel("ShaderGraph").AppendLabel("Mesh"),
                VFXLibraryStringHelper.Separator("Output Basic", 2),
                typeof(VFXComposedParticleOutput),
                () => new ParticleTopologyMesh());
        }
    }

    [VFXInfo(variantProvider = typeof(VFXTopologyProvider), synonyms = new []{ "Shape" })]
    sealed class VFXComposedParticleOutput : VFXAbstractComposedParticleOutput
    {
        VFXComposedParticleOutput() : base(false) { }
    }
}
