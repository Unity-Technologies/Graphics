using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXDecalOutput : VFXAbstractParticleOutput
    {
        public override string name => "Output Particle".AppendLabel("Forward Decal", false);
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleDecal"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleHexahedronOutput; } }
        public override bool supportsUV { get { return true; } }
        public override CullMode defaultCullMode { get { return CullMode.Back; } }
        public override bool hasShadowCasting { get { return false; } }
        public override bool supportSoftParticles { get { return false; } }
        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var input in base.inputProperties)
                    yield return input;

                yield return new VFXPropertyWithValue(new VFXProperty(GetFlipbookType(), "mainTexture", new TooltipAttribute("Specifies the base color (RGB) and opacity (A) of the particle.")), (usesFlipbook ? null : VFXResources.defaultResources.particleTexture));
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            cullMode = CullMode.Back;
            zTestMode = ZTestMode.LEqual;
            zWriteMode = ZWriteMode.Off;
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return "cullMode";
                yield return "zWriteMode";
                yield return "zTestMode";
                yield return "castShadows";
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            yield return slotExpressions.First(o => o.name == "mainTexture");
        }
    }
}
