namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal enum RenderStateType
    {
        Cull,
        Blend,
        BlendOp,
        ZTest,
        ZWrite,
        ColorMask,
        ZClip,
        Stencil,
    }

    [GenerationAPI]
    internal struct RenderStateDescriptor
    {
        public string value;
        public RenderStateType type;
    }

    [GenerationAPI]
    internal static class RenderState
    {
        public static RenderStateDescriptor Cull(Cull value) => new RenderStateDescriptor { type = RenderStateType.Cull, value = $"Cull {value}" };
        public static RenderStateDescriptor Cull(string value) => new RenderStateDescriptor { type = RenderStateType.Cull, value = $"Cull {value}" };
        public static RenderStateDescriptor Blend(Blend src, Blend dst) => new RenderStateDescriptor { type = RenderStateType.Blend, value = $"Blend {src} {dst}" };
        public static RenderStateDescriptor Blend(string src, string dst) => new RenderStateDescriptor { type = RenderStateType.Blend, value = $"Blend {src} {dst}" };
        public static RenderStateDescriptor Blend(Blend src, Blend dst, Blend alphaSrc, Blend alphaDst) => new RenderStateDescriptor { type = RenderStateType.Blend, value = $"Blend {src} {dst}, {alphaSrc} {alphaDst}" };
        public static RenderStateDescriptor Blend(string src, string dst, string alphaSrc, string alphaDst) => new RenderStateDescriptor { type = RenderStateType.Blend, value = $"Blend {src} {dst}, {alphaSrc} {alphaDst}" };
        public static RenderStateDescriptor Blend(string value) => new RenderStateDescriptor { type = RenderStateType.Blend, value = value };
        public static RenderStateDescriptor BlendOp(BlendOp op) => new RenderStateDescriptor { type = RenderStateType.BlendOp, value = $"BlendOp {op}" };
        public static RenderStateDescriptor BlendOp(string op) => new RenderStateDescriptor { type = RenderStateType.BlendOp, value = $"BlendOp {op}" };
        public static RenderStateDescriptor BlendOp(BlendOp op, BlendOp opAlpha) => new RenderStateDescriptor { type = RenderStateType.BlendOp, value = $"BlendOp {op}, {opAlpha}" };
        public static RenderStateDescriptor BlendOp(string op, string opAlpha) => new RenderStateDescriptor { type = RenderStateType.BlendOp, value = $"BlendOp {op}, {opAlpha}" };
        public static RenderStateDescriptor ZTest(ZTest value) => new RenderStateDescriptor { type = RenderStateType.ZTest, value = $"ZTest {value}" };
        public static RenderStateDescriptor ZTest(string value) => new RenderStateDescriptor { type = RenderStateType.ZTest, value = $"ZTest {value}" };
        public static RenderStateDescriptor ZWrite(ZWrite value) => new RenderStateDescriptor { type = RenderStateType.ZWrite, value = $"ZWrite {value}" };
        public static RenderStateDescriptor ZWrite(string value) => new RenderStateDescriptor { type = RenderStateType.ZWrite, value = $"ZWrite {value}" };
        public static RenderStateDescriptor ZClip(string value) => new RenderStateDescriptor { type = RenderStateType.ZClip, value = $"ZClip {value}" };
        public static RenderStateDescriptor ColorMask(string value) => new RenderStateDescriptor { type = RenderStateType.ColorMask, value = $"{value}" };
        public static RenderStateDescriptor Stencil(StencilDescriptor value) => new RenderStateDescriptor { type = RenderStateType.Stencil, value = value.ToShaderString() };
    }
}
