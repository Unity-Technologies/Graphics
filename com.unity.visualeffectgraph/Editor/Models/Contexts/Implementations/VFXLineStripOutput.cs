using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(experimental = true)]
    class VFXLineStripOutput : VFXAbstractParticleOutput
    {
        protected VFXLineStripOutput() : base(true) { }
        public override string name { get { return "Output ParticleStrip Line"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleLinesHW"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleLineOutput; } }
        public override bool implementsMotionVector { get { return true; } }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return "cullMode";
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            cullMode = CullMode.Off;
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
            }
        }
    }
}
