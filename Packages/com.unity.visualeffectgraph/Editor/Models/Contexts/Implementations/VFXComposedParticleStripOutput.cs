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
                typeof(VFXComposedParticleStripOutput),
                new[] { new KeyValuePair<string, object>("m_Topology", new ParticleTopologyQuadStrip()) });
        }
    }

    [VFXInfo(variantProvider = typeof(VFXStripTopologyProvider))]
    sealed class VFXComposedParticleStripOutput : VFXAbstractComposedParticleOutput
    {
        VFXComposedParticleStripOutput() : base(true) { }

        internal override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            base.GenerateErrors(manager);
            foreach (var attributeInfo in GetAttributesInfos())
            {
                if (attributeInfo.mode.HasFlag(VFXAttributeMode.Write) && attributeInfo.attrib.Equals(VFXAttribute.Position))
                {
                    manager.RegisterError("WritePositionInStrip", VFXErrorType.Warning, VFXQuadStripOutput.WriteToPositionMessage);
                    break;
                }
            }
        }
    }
}
