using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        [Serializable]
        public class Settings
        {
            public VFXIntegrationMode integration = VFXIntegrationMode.Euler;
        }

        public VFXBasicUpdate() : base(VFXContextType.kUpdate, VFXDataType.kParticle, VFXDataType.kParticle) {}
        public override string name { get { return "Update"; } }

        public override VFXCodeGenerator codeGenerator
        {
            get
            {
                return new VFXCodeGenerator("VFXUpdate");
            }
        }

        protected override IEnumerable<VFXBlock> implicitPostBlock
        {
            get
            {
                var implicitPostBlock = new List<VFXBlock>();
                var settings = GetSettings<Settings>();
                var data = GetData();
                if (settings.integration != VFXIntegrationMode.None && data.AttributeExists(VFXAttribute.Velocity))
                {
                    var eulerIntergration = CreateInstance<VFXEulerIntegration>();
                    implicitPostBlock.Add(eulerIntergration);
                }

                if (GetData().AttributeExists(VFXAttribute.Lifetime))
                {
                    var ageAndDie = CreateInstance<VFXAgeAndDie>();
                    implicitPostBlock.Add(ageAndDie);
                }

                return implicitPostBlock;
            }
        }
    }
}
