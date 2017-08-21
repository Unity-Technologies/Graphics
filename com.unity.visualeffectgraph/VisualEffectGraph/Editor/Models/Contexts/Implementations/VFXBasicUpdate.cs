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
                return implicitPostBlock;
            }
        }

        public override IEnumerable<VFXAttributeInfo> optionalAttributes
        {
            get
            {
                if (GetData().AttributeExists(VFXAttribute.Velocity)) // If there is velocity, position becomes writable
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                if (GetData().AttributeExists(VFXAttribute.Lifetime)) // If there is a lifetime, aging is enabled
                    yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.ReadWrite);
            }
        }
    }
}
