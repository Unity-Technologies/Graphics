using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalRenderStates
    {
        public static readonly RenderStateCollection Default = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.ZWrite(ZWrite.Off), new FieldCondition(Fields.SurfaceTransparent, true) },
            { RenderState.Cull(Cull.Back), new FieldCondition(Fields.DoubleSided, false) },
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendPremultiply, true) },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(Fields.BlendAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(Fields.BlendMultiply, true) },
        };

        public static readonly RenderStateCollection GBufferLit = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.ZWrite(ZWrite.Off), new FieldCondition(Fields.SurfaceTransparent, true) },
            { RenderState.Cull(Cull.Back), new FieldCondition(Fields.DoubleSided, false) },
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendPremultiply, true) },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(Fields.BlendAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(Fields.BlendMultiply, true) },
            { RenderState.Stencil(
                // [Stencil] Bit 5-6 material type. 00 = unlit/bakedLit, 01 = Lit, 10 = SimpleLit",
                // This is an Lit material.",
                new StencilDescriptor {
                    WriteMask = "96", // 0b01100000
                    Ref = "32",       // 0b00100000
                    Comp = "Always",
                    ZFail = "Keep",
                    Fail = "Keep",
                    Pass = "Replace"
                }),
                new FieldCondition(Fields.SurfaceOpaque, true)
            },
        };

        public static readonly RenderStateCollection ShadowCasterMeta = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Cull(Cull.Back), new FieldCondition(Fields.DoubleSided, false) },
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendPremultiply, true) },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(Fields.BlendAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(Fields.BlendMultiply, true) },
        };

        public static readonly RenderStateCollection DepthOnly = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Cull(Cull.Back), new FieldCondition(Fields.DoubleSided, false) },
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.ColorMask("ColorMask 0") },
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendPremultiply, true) },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(Fields.BlendAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(Fields.BlendMultiply, true) },
        };
    }
}
