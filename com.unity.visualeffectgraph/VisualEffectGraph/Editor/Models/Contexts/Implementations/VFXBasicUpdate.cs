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
        public override string renderLoopCommonInclude { get { return "VFXShaders/Common/VFXCommonCompute.cginc"; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                if (GetData().IsCurrentAttributeRead(VFXAttribute.OldPosition))
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.OldPosition, VFXAttributeMode.Write);
                }
            }
        }

        protected override IEnumerable<VFXBlock> implicitPostBlock
        {
            get
            {
                var data = GetData();

                if (integration != VFXIntegrationMode.None && data.IsCurrentAttributeWritten(VFXAttribute.Velocity))
                    yield return CreateInstance<EulerIntegration>();

                if (GetData().IsCurrentAttributeWritten(VFXAttribute.Lifetime))
                    yield return CreateInstance<AgeAndDie>();
            }
        }
    }
}
