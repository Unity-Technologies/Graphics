
//seongdae;new-crp
namespace UnityEngine.Rendering.Universal
{
    public enum DeformableRenderPass
    {
        DepthPrepass = 0,
        ScreenSpaceShadowResolvePass,
        RenderOpaqueForwardPass,
        RenderTransparentForwardpass,

        Invariable,
    }

    public enum DeformingType
    {
        BeforeBuiltinRenderPass = 0,
        SwitchBuiltinRenderPass,
        AfterBuiltinRenderPass,
    }

    public struct DeformingRenderPass
    {
        public ScriptableRenderPass PreRenderPass;
        public ScriptableRenderPass PostRenderPass;

        public ScriptableRenderPass Switchable;
    }
}
//seongdae;new-crp
