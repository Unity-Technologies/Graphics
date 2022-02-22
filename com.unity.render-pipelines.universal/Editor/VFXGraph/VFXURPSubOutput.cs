#if HAS_VFX_GRAPH

namespace UnityEditor.VFX.URP
{
    class VFXURPSubOutput : VFXSRPSubOutput
    {
        public override bool supportsMotionVector => true;
    }
}
#endif
