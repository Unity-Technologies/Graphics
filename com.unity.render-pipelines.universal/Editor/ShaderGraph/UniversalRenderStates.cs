using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalRenderStates
    {
        public static readonly RenderStateCollection Default = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On), new FieldCondition(DefaultFields.SurfaceOpaque, true) },
            { RenderState.ZWrite(ZWrite.Off), new FieldCondition(DefaultFields.SurfaceTransparent, true) },
            { RenderState.Cull(Cull.Back), new FieldCondition(DefaultFields.DoubleSided, false) },
            { RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true) },
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(DefaultFields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendPremultiply, true) },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(DefaultFields.BlendAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(DefaultFields.BlendMultiply, true) },
        };

        public static readonly RenderStateCollection ShadowCasterMeta = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Cull(Cull.Back), new FieldCondition(DefaultFields.DoubleSided, false) },
            { RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true) },
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(DefaultFields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendPremultiply, true) },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(DefaultFields.BlendAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(DefaultFields.BlendMultiply, true) },
        };

        public static readonly RenderStateCollection DepthOnly = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Cull(Cull.Back), new FieldCondition(DefaultFields.DoubleSided, false) },
            { RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true) },
            { RenderState.ColorMask("ColorMask 0") },
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(DefaultFields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendPremultiply, true) },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(DefaultFields.BlendAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(DefaultFields.BlendMultiply, true) },
        };
    }
}
