using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(name = "Output Particle|Cube", category = "#5Output Debug", experimental = true, synonyms = new []{ "Box" })]
    class VFXBasicCubeOutput : VFXAbstractParticleOutput
    {
        public override string name => "Output Particle\nCube";
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleBasicCube"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleHexahedronOutput; } }

        public override bool supportsUV { get { return true; } }
        public override bool implementsMotionVector { get { return true; } }

        public override CullMode defaultCullMode { get { return CullMode.Back; } }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            yield return slotExpressions.First(o => o.name == "mainTexture");
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var input in base.inputProperties)
                    yield return input;

                yield return new VFXPropertyWithValue(new VFXProperty(GetFlipbookType(), "mainTexture", new TooltipAttribute("Specifies the base color (RGB) and opacity (A) of the particle.")), (usesFlipbook ? null : VFXResources.defaultResources.particleTexture));
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return nameof(colorMapping);
                yield return nameof(enableRayTracing);
            }
        }

        protected override IEnumerable<string> untransferableSettings
        {
            get
            {
                foreach (var setting in base.untransferableSettings)
                {
                    yield return setting;
                }
                yield return nameof(enableRayTracing);
            }
        }
    }
}
