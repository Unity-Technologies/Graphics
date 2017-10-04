using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX.Block;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicUpdate : VFXContext
    {
        public enum VFXIntegrationMode
        {
            Euler,
            None
        }

        [VFXSetting]
        public VFXIntegrationMode integration = VFXIntegrationMode.Euler;

        public VFXBasicUpdate() : base(VFXContextType.kUpdate, VFXDataType.kParticle, VFXDataType.kParticle) {}
        public override string name { get { return "Update"; } }
        public override string codeGeneratorTemplate { get { return "VFXShaders/VFXUpdate"; } }
        public override bool codeGeneratorCompute { get { return true; } }
        public override VFXTaskType taskType { get { return VFXTaskType.kUpdate; } }

        protected override IEnumerable<VFXBlock> implicitPostBlock
        {
            get
            {
                var data = GetData();

                if (integration != VFXIntegrationMode.None && data.IsAttributeWritten(VFXAttribute.Velocity))
                    yield return CreateInstance<EulerIntegration>();

                if (GetData().IsAttributeWritten(VFXAttribute.Lifetime))
                    yield return CreateInstance<AgeAndDie>();
            }
        }
    }
}
