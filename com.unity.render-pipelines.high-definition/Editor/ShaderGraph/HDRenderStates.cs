using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDRenderStates
    {
        static class Uniforms
        {
            public static readonly string srcBlend = "[_SrcBlend]";
            public static readonly string dstBlend = "[_DstBlend]";
            public static readonly string alphaSrcBlend = "[_AlphaSrcBlend]";
            public static readonly string alphaDstBlend = "[_AlphaDstBlend]";
            public static readonly string cullMode = "[_CullMode]";
            public static readonly string cullModeForward = "[_CullModeForward]";
            public static readonly string zTestDepthEqualForOpaque = "[_ZTestDepthEqualForOpaque]";
            public static readonly string zTestTransparent = "[_ZTestTransparent]";
            public static readonly string zTestGBuffer = "[_ZTestGBuffer]";
            public static readonly string zWrite = "[_ZWrite]";
            public static readonly string zClip = "[_ZClip]";
            public static readonly string stencilWriteMaskDepth = "[_StencilWriteMaskDepth]";
            public static readonly string stencilRefDepth = "[_StencilRefDepth]";
            public static readonly string stencilWriteMaskMV = "[_StencilWriteMaskMV]";
            public static readonly string stencilRefMV = "[_StencilRefMV]";
            public static readonly string stencilWriteMask = "[_StencilWriteMask]";
            public static readonly string stencilRef = "[_StencilRef]";
            public static readonly string stencilWriteMaskGBuffer = "[_StencilWriteMaskGBuffer]";
            public static readonly string stencilRefGBuffer = "[_StencilRefGBuffer]";
            public static readonly string stencilRefDistortionVec = "[_StencilRefDistortionVec]";
            public static readonly string stencilWriteMaskDistortionVec = "[_StencilWriteMaskDistortionVec]";
        }

        readonly static string[] s_DecalColorMasks = new string[8]
        {
            "ColorMask 0 2 ColorMask 0 3",      // nothing
            "ColorMask R 2 ColorMask R 3",      // metal
            "ColorMask G 2 ColorMask G 3",      // AO
            "ColorMask RG 2 ColorMask RG 3",    // metal + AO
            "ColorMask BA 2 ColorMask 0 3",     // smoothness
            "ColorMask RBA 2 ColorMask R 3",    // metal + smoothness
            "ColorMask GBA 2 ColorMask G 3",    // AO + smoothness
            "ColorMask RGBA 2 ColorMask RG 3",  // metal + AO + smoothness
        };

        // --------------------------------------------------
        // META

        public static RenderStateCollection Meta = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off) },
        };

        // --------------------------------------------------
        // Shadow Caster

        public static RenderStateCollection ShadowCasterUnlit = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        public static RenderStateCollection ShadowCasterPBR = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.Blend(Blend.One, Blend.Zero) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        public static RenderStateCollection HDShadowCaster = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ZClip(Uniforms.zClip) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        public static RenderStateCollection HDBlendShadowCaster = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.Zero) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ZClip(Uniforms.zClip) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        public static RenderStateCollection StackLitShadowCaster = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.Zero) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ZClip(Uniforms.zClip) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        // --------------------------------------------------
        // Scene Selection

        public static RenderStateCollection SceneSelection = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.ZWrite(ZWrite.On), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.ZWrite(ZWrite.Off), new FieldCondition(Fields.SurfaceTransparent, true) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        public static RenderStateCollection HDSceneSelection = new RenderStateCollection
        {
            { RenderState.ColorMask("ColorMask 0") },
        };

        public static RenderStateCollection HDUnlitSceneSelection = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        // --------------------------------------------------
        // Depth Forward Only

        // Caution: When using MSAA we have normal and depth buffer bind.
        // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
        // This is not a problem in no MSAA mode as there is no buffer bind
        public static RenderStateCollection DepthForwardOnly = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.ZWrite(ZWrite.On), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.ZWrite(ZWrite.Off), new FieldCondition(Fields.SurfaceTransparent, true) },
            { RenderState.ColorMask("ColorMask 0 0") },
        };

        // Caution: When using MSAA we have normal and depth buffer bind.
        // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
        // This is not a problem in no MSAA mode as there is no buffer bind
        public static RenderStateCollection HDDepthForwardOnly = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ColorMask("ColorMask 0 0") },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMaskDepth,
                Ref = Uniforms.stencilRefDepth,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        // --------------------------------------------------
        // Depth Only

        public static RenderStateCollection DepthOnly = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = $"{ 0 | (int)StencilUsage.TraceReflectionRay}",
                Ref = $"{0 | (int)StencilUsage.TraceReflectionRay}",
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection HDDepthOnly = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMaskDepth,
                Ref = Uniforms.stencilRefDepth,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection HairDepthOnly = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMaskDepth,
                Ref = Uniforms.stencilRefDepth,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        // --------------------------------------------------
        // Motion Vectors

        // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
        // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
        // This is not a problem in no MSAA mode as there is no buffer bind
        public static RenderStateCollection UnlitMotionVectors = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.ColorMask("ColorMask 0 1") },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = $"{(int)StencilUsage.ObjectMotionVector}",
                Ref = $"{(int)StencilUsage.ObjectMotionVector}",
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection PBRMotionVectors = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = $"{0 | (int)StencilUsage.TraceReflectionRay | (int)StencilUsage.ObjectMotionVector}",
                Ref = $"{ 0 | (int)StencilUsage.TraceReflectionRay | (int)StencilUsage.ObjectMotionVector}",
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection HDMotionVectors = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMaskMV,
                Ref = Uniforms.stencilRefMV,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
        // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
        // This is not a problem in no MSAA mode as there is no buffer bind
        public static RenderStateCollection HDUnlitMotionVectors = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ColorMask("ColorMask 0 1") },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMaskMV,
                Ref = Uniforms.stencilRefMV,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection HairMotionVectors = new RenderStateCollection
        {
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMaskMV,
                Ref = Uniforms.stencilRefMV,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        // --------------------------------------------------
        // Forward

        public static RenderStateCollection UnlitForward = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceTransparent, true),
                new FieldCondition(Fields.BlendAlpha, true) } },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceTransparent, true),
                new FieldCondition(Fields.BlendAdd, true) } },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceTransparent, true),
                new FieldCondition(Fields.BlendPremultiply, true) } },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceTransparent, true),
                new FieldCondition(Fields.BlendMultiply, true) } },

            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.ZWrite(ZWrite.On), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.ZWrite(ZWrite.Off), new FieldCondition(Fields.SurfaceTransparent, true) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = $"{(int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering}",
                Ref = $"{(int)StencilUsage.Clear}",
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection PBRForward = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceTransparent, true),
                new FieldCondition(Fields.BlendAlpha, true) } },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceTransparent, true),
                new FieldCondition(Fields.BlendAdd, true) } },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceTransparent, true),
                new FieldCondition(Fields.BlendPremultiply, true) } },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceTransparent, true),
                new FieldCondition(Fields.BlendMultiply, true) } },

            { RenderState.ZTest(ZTest.Equal), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceOpaque, true),
                new FieldCondition(Fields.AlphaTest, true) } },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = $"{(int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering}",
                Ref = $"{(int)StencilUsage.Clear}",
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection HDUnlitForward = new RenderStateCollection
        {
            { RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend, Uniforms.alphaSrcBlend, Uniforms.alphaDstBlend) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.ZTest(Uniforms.zTestTransparent) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMask,
                Ref = Uniforms.stencilRef,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection HDForward = new RenderStateCollection
        {
            { RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend, Uniforms.alphaSrcBlend, Uniforms.alphaDstBlend) },
            { RenderState.Cull(Uniforms.cullModeForward) },
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.ZTest(Uniforms.zTestDepthEqualForOpaque), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceOpaque, true),
                new FieldCondition(Fields.AlphaTest, false)
            } },
            { RenderState.ZTest(Uniforms.zTestDepthEqualForOpaque), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceOpaque, false),
            } },
            { RenderState.ZTest(ZTest.Equal), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceOpaque, true),
                new FieldCondition(Fields.AlphaTest, true)
            } },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMask,
                Ref = Uniforms.stencilRef,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection HDForwardColorMask = new RenderStateCollection
        {
            { RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend, Uniforms.alphaSrcBlend, Uniforms.alphaDstBlend) },
            { RenderState.Cull(Uniforms.cullModeForward) },
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.ZTest(Uniforms.zTestDepthEqualForOpaque), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceOpaque, true),
                new FieldCondition(Fields.AlphaTest, false)
            } },
            { RenderState.ZTest(Uniforms.zTestDepthEqualForOpaque), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceOpaque, false),
            } },
            { RenderState.ZTest(ZTest.Equal), new FieldCondition[] {
                new FieldCondition(Fields.SurfaceOpaque, true),
                new FieldCondition(Fields.AlphaTest, true)
            } },
            { RenderState.ColorMask("ColorMask [_ColorMaskTransparentVel] 1") },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMask,
                Ref = Uniforms.stencilRef,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        // --------------------------------------------------
        // GBuffer

        public static RenderStateCollection PBRGBuffer = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.ZTest(ZTest.Equal) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = $"{ 0 | (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering | (int)StencilUsage.TraceReflectionRay}",
                Ref = $"{0 | (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.TraceReflectionRay}",
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection HDLitGBuffer = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZTest(Uniforms.zTestGBuffer) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMaskGBuffer,
                Ref = Uniforms.stencilRefGBuffer,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        // --------------------------------------------------
        // Distortion

        public static RenderStateCollection HDUnlitDistortion = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(HDFields.DistortionAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.DstAlpha, Blend.Zero), new FieldCondition(HDFields.DistortionMultiply, true) },
            { RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(HDFields.DistortionReplace, true) },
            { RenderState.BlendOp(BlendOp.Add, BlendOp.Add) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.Off) },
            { RenderState.ZTest(ZTest.Always), new FieldCondition(HDFields.DistortionDepthTest, false) },
            { RenderState.ZTest(ZTest.LEqual), new FieldCondition(HDFields.DistortionDepthTest, true) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMaskDistortionVec,
                Ref = Uniforms.stencilRefDistortionVec,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection HDLitDistortion = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(HDFields.DistortionAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.DstAlpha, Blend.Zero), new FieldCondition(HDFields.DistortionMultiply, true) },
            { RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(HDFields.DistortionReplace, true) },
            { RenderState.BlendOp(BlendOp.Add, BlendOp.Add) },
            { RenderState.ZWrite(ZWrite.Off) },
            { RenderState.ZTest(ZTest.Always), new FieldCondition(HDFields.DistortionDepthTest, false) },
            { RenderState.ZTest(ZTest.LEqual), new FieldCondition(HDFields.DistortionDepthTest, true) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMaskDistortionVec,
                Ref = Uniforms.stencilRefDistortionVec,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection StackLitDistortion = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(HDFields.DistortionAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.DstAlpha, Blend.Zero), new FieldCondition(HDFields.DistortionMultiply, true) },
            { RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(HDFields.DistortionReplace, true) },
            { RenderState.BlendOp(BlendOp.Add, BlendOp.Add) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.Off) },
            { RenderState.ZTest(ZTest.Always), new FieldCondition(HDFields.DistortionDepthTest, false) },
            { RenderState.ZTest(ZTest.LEqual), new FieldCondition(HDFields.DistortionDepthTest, true) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = $"{(int)StencilUsage.DistortionVectors}",
                Ref = $"{(int)StencilUsage.DistortionVectors}",
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        // --------------------------------------------------
        // Transparent Depth Prepass & Postpass

        public static RenderStateCollection HDTransparentDepthPrePostPass = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.Zero) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        // --------------------------------------------------
        // Transparent Backface

        public static RenderStateCollection HDTransparentBackface = new RenderStateCollection
        {
            { RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend, Uniforms.alphaSrcBlend, Uniforms.alphaDstBlend) },
            { RenderState.Cull(Cull.Front) },
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.ZTest(Uniforms.zTestTransparent) },
            { RenderState.ColorMask("ColorMask [_ColorMaskTransparentVel] 1") },
        };

        // --------------------------------------------------
        // Decal

        public static RenderStateCollection DecalProjector3RT = new RenderStateCollection
        {
            { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha") },
            { RenderState.Cull(Cull.Front) },
            { RenderState.ZTest(ZTest.Greater) },
            { RenderState.ZWrite(ZWrite.Off) },
            { RenderState.ColorMask(s_DecalColorMasks[4]) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = ((int)StencilUsage.Decals).ToString(),
                Ref = ((int)StencilUsage.Decals).ToString(),
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection DecalProjector4RT = new RenderStateCollection
        {
            { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 3 Zero OneMinusSrcColor") },
            { RenderState.Cull(Cull.Front) },
            { RenderState.ZTest(ZTest.Greater) },
            { RenderState.ZWrite(ZWrite.Off) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = ((int)StencilUsage.Decals).ToString(),
                Ref = ((int)StencilUsage.Decals).ToString(),
                Comp = "Always",
                Pass = "Replace",
            }) },

            // ColorMask per Affects Channel
            { RenderState.ColorMask(s_DecalColorMasks[0]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, false),
                new FieldCondition(HDFields.AffectsAO, false),
                new FieldCondition(HDFields.AffectsSmoothness, false) } },
            { RenderState.ColorMask(s_DecalColorMasks[1]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, true),
                new FieldCondition(HDFields.AffectsAO, false),
                new FieldCondition(HDFields.AffectsSmoothness, false) } },
            { RenderState.ColorMask(s_DecalColorMasks[2]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, false),
                new FieldCondition(HDFields.AffectsAO, true),
                new FieldCondition(HDFields.AffectsSmoothness, false) } },
            { RenderState.ColorMask(s_DecalColorMasks[3]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, true),
                new FieldCondition(HDFields.AffectsAO, true),
                new FieldCondition(HDFields.AffectsSmoothness, false) } },
            { RenderState.ColorMask(s_DecalColorMasks[4]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, false),
                new FieldCondition(HDFields.AffectsAO, false),
                new FieldCondition(HDFields.AffectsSmoothness, true) } },
            { RenderState.ColorMask(s_DecalColorMasks[5]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, true),
                new FieldCondition(HDFields.AffectsAO, false),
                new FieldCondition(HDFields.AffectsSmoothness, true) } },
            { RenderState.ColorMask(s_DecalColorMasks[6]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, false),
                new FieldCondition(HDFields.AffectsAO, true),
                new FieldCondition(HDFields.AffectsSmoothness, true) } },
            { RenderState.ColorMask(s_DecalColorMasks[7]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, true),
                new FieldCondition(HDFields.AffectsAO, true),
                new FieldCondition(HDFields.AffectsSmoothness, true) } },
        };

        public static RenderStateCollection DecalProjectorEmissive = new RenderStateCollection
        {
            { RenderState.Blend("Blend 0 SrcAlpha One") },
            { RenderState.Cull(Cull.Front) },
            { RenderState.ZTest(ZTest.Greater) },
            { RenderState.ZWrite(ZWrite.Off) },
        };

        public static RenderStateCollection DecalMesh3RT = new RenderStateCollection
        {
            { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha") },
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.Off) },
            { RenderState.ColorMask(s_DecalColorMasks[4]) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = ((int)StencilUsage.Decals).ToString(),
                Ref = ((int)StencilUsage.Decals).ToString(),
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection DecalMesh4RT = new RenderStateCollection
        {
            { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 3 Zero OneMinusSrcColor") },
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.Off) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = ((int)StencilUsage.Decals).ToString(),
                Ref = ((int)StencilUsage.Decals).ToString(),
                Comp = "Always",
                Pass = "Replace",
            }) },

            // ColorMask per Affects Channel
            { RenderState.ColorMask(s_DecalColorMasks[0]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, false),
                new FieldCondition(HDFields.AffectsAO, false),
                new FieldCondition(HDFields.AffectsSmoothness, false) } },
            { RenderState.ColorMask(s_DecalColorMasks[1]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, true),
                new FieldCondition(HDFields.AffectsAO, false),
                new FieldCondition(HDFields.AffectsSmoothness, false) } },
            { RenderState.ColorMask(s_DecalColorMasks[2]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, false),
                new FieldCondition(HDFields.AffectsAO, true),
                new FieldCondition(HDFields.AffectsSmoothness, false) } },
            { RenderState.ColorMask(s_DecalColorMasks[3]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, true),
                new FieldCondition(HDFields.AffectsAO, true),
                new FieldCondition(HDFields.AffectsSmoothness, false) } },
            { RenderState.ColorMask(s_DecalColorMasks[4]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, false),
                new FieldCondition(HDFields.AffectsAO, false),
                new FieldCondition(HDFields.AffectsSmoothness, true) } },
            { RenderState.ColorMask(s_DecalColorMasks[5]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, true),
                new FieldCondition(HDFields.AffectsAO, false),
                new FieldCondition(HDFields.AffectsSmoothness, true) } },
            { RenderState.ColorMask(s_DecalColorMasks[6]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, false),
                new FieldCondition(HDFields.AffectsAO, true),
                new FieldCondition(HDFields.AffectsSmoothness, true) } },
            { RenderState.ColorMask(s_DecalColorMasks[7]), new FieldCondition[] {
                new FieldCondition(HDFields.AffectsMetal, true),
                new FieldCondition(HDFields.AffectsAO, true),
                new FieldCondition(HDFields.AffectsSmoothness, true) } },
        };

        public static RenderStateCollection DecalMeshEmissive = new RenderStateCollection
        {
            { RenderState.Blend("Blend 0 SrcAlpha One") },
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.Off) },
        };

        public static RenderStateCollection DecalPreview = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
        };
    }
}
