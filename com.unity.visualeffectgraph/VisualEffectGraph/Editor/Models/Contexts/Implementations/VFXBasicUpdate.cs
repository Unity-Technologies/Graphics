using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicUpdate : VFXContext
    {
        public VFXBasicUpdate() : base(VFXContextType.kUpdate, VFXDataType.kParticle, VFXDataType.kParticle) {}
        public override string name { get { return "Update"; } }

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
