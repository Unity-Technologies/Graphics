using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXQuadOutput : VFXContext
    {
        public VFXQuadOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) {}
        public override string name { get { return "Quad Output"; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            public Texture2D texture;
        }
    }
}
