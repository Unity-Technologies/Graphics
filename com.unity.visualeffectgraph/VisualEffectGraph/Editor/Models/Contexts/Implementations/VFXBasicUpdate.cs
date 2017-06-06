using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicUpdate : VFXContext
    {
        public VFXBasicUpdate() : base(VFXContextType.kUpdate, VFXDataType.kParticle, VFXDataType.kParticle) {}
        public override string name { get { return "Update"; } }
        public override IEnumerable<VFXAttributeInfo> attributes 
        { 
            get
            {
                yield return new VFXAttributeInfo("position", VFXValueType.kFloat3, VFXAttributeMode.ReadWrite);
            }
        }
    }
}
