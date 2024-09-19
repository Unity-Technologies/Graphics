using System.Collections.Generic;

namespace UnityEditor.VFX
{
    class VFXStripTopologyProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new VFXComposedParticleOutputVariant(
                "Output ParticleStrip".AppendLabel("ShaderGraph").AppendLabel("Quad"),
                VFXLibraryStringHelper.Separator("Output Strip", 3),
                typeof(VFXComposedParticleStripOutput),
                () => new ParticleTopologyQuadStrip());
        }
    }

    [VFXInfo(variantProvider = typeof(VFXStripTopologyProvider), synonyms = new []{ "Trail", "Ribbon" })]
    sealed class VFXComposedParticleStripOutput : VFXAbstractComposedParticleOutput
    {
        VFXComposedParticleStripOutput() : base(true) { }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);
            foreach (var attributeInfo in GetAttributesInfos())
            {
                if (attributeInfo.mode.HasFlag(VFXAttributeMode.Write) && attributeInfo.attrib.Equals(VFXAttribute.Position))
                {
                    report.RegisterError("WritePositionInStrip", VFXErrorType.Warning, VFXQuadStripOutput.WriteToPositionMessage, this);
                    break;
                }
            }
        }
    }
}
