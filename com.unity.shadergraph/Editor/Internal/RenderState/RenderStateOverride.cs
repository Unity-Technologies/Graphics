namespace UnityEditor.ShaderGraph.Internal
{
    public class RenderStateOverride
    {
        public enum Type
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

        public Type type { get; }
        public string value { get; }
        public int priority { get; }
        public IField[] requiredFields { get; }

        internal RenderStateOverride(RenderStateOverride.Type type, string value, int priority, IField[] requiredFields)
        {
            this.type = type;
            this.value = value;
            this.priority = priority;
            this.requiredFields = requiredFields;
        }

        public static RenderStateOverride Cull(Cull value, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.Cull, $"Cull {value}", priority, requiredFields);
        }

        public static RenderStateOverride Cull(string value, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.Cull, $"Cull {value}", priority, requiredFields);
        }

        public static RenderStateOverride Blend(Blend src, Blend dst, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.Blend, $"Blend {src} {dst}", priority, requiredFields);
        }

        public static RenderStateOverride Blend(string src, string dst, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.Blend, $"Blend {src} {dst}", priority, requiredFields);
        }

        public static RenderStateOverride Blend(Blend src, Blend dst, Blend alphaSrc, Blend alphaDst, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.Blend, $"Blend {src} {dst}, {alphaSrc} {alphaDst}", priority, requiredFields);
        }

        public static RenderStateOverride Blend(string src, string dst, string alphaSrc, string alphaDst, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.Blend, $"Blend {src} {dst}, {alphaSrc} {alphaDst}", priority, requiredFields);
        }

        public static RenderStateOverride BlendOp(BlendOp op, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.BlendOp, $"BlendOp {op}", priority, requiredFields);
        }

        public static RenderStateOverride BlendOp(string op, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.BlendOp, $"BlendOp {op}", priority, requiredFields);
        }

        public static RenderStateOverride BlendOp(BlendOp op, BlendOp opAlpha, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.BlendOp, $"BlendOp {op}, {opAlpha}", priority, requiredFields);
        }

        public static RenderStateOverride BlendOp(string op, string opAlpha, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.BlendOp, $"BlendOp {op}, {opAlpha}", priority, requiredFields);
        }

        public static RenderStateOverride ZTest(ZTest value, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.ZTest, $"ZTest {value}", priority, requiredFields);
        }

        public static RenderStateOverride ZTest(string value, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.ZTest, $"ZTest {value}", priority, requiredFields);
        }

        public static RenderStateOverride ZWrite(ZWrite value, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.ZWrite, $"ZWrite {value}", priority, requiredFields);
        }

        public static RenderStateOverride ZWrite(string value, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.ZWrite, $"ZWrite {value}", priority, requiredFields);
        }

        public static RenderStateOverride ZClip(string value, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.ZClip, $"ZClip {value}", priority, requiredFields);
        }

        public static RenderStateOverride ColorMask(string value, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.ColorMask, $"ColorMask {value}", priority, requiredFields);
        }        

        public static RenderStateOverride Stencil(Stencil value, int priority, IField[] requiredFields = null)
        {
            return new RenderStateOverride(Type.Stencil, value.ToString(), priority, requiredFields);
        }
    }
}
