using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXQuadOutput : VFXContext
    {
        public VFXQuadOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) { }
        public override string name { get { return "Quad Output"; } }

        public class InputProperties
        {
            public Texture2D texture;
        }
    }
}