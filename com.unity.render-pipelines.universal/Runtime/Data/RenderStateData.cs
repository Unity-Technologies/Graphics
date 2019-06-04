namespace UnityEngine.Rendering.LWRP
{
    [System.Serializable]
    public class StencilStateData
    {
        public bool overrideStencilState = false;
        public int stencilReference = 0;
        public CompareFunction stencilCompareFunction = CompareFunction.Always;
        public StencilOp passOperation = StencilOp.Keep;
        public StencilOp failOperation = StencilOp.Keep;
        public StencilOp zFailOperation = StencilOp.Keep;
    }
}