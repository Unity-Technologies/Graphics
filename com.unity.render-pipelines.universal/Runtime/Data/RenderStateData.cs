using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    [System.Serializable]
    [MovedFrom("UnityEngine.Rendering.LWRP")] public class StencilStateData
    {
        public bool overrideStencilState = false;
        public int stencilReference = 0;
        public CompareFunction stencilCompareFunction = CompareFunction.Always;
        public StencilOp passOperation = StencilOp.Keep;
        public StencilOp failOperation = StencilOp.Keep;
        public StencilOp zFailOperation = StencilOp.Keep;
    }
}
