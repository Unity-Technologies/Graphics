using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Experimental.Rendering.Universal;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal
{
    class UniversalMeshTarget : ITargetVariant<MeshTarget>
    {
        public string displayName => "Universal";
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();

        public bool Validate(RenderPipelineAsset pipelineAsset)
        {
            return pipelineAsset is UniversalRenderPipelineAsset;
        }

        public bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader)
        {
            switch(masterNode)
            {
                case PBRMasterNode pbrMasterNode:
                    subShader = new UniversalPBRSubShader();
                    return true;
                case UnlitMasterNode unlitMasterNode:
                    subShader = new UniversalUnlitSubShader();
                    return true;
                case SpriteLitMasterNode spriteLitMasterNode:
                    subShader = new UniversalSpriteLitSubShader();
                    return true;
                case SpriteUnlitMasterNode spriteUnlitMasterNode:
                    subShader = new UniversalSpriteUnlitSubShader();
                    return true;
                default:
                    subShader = null;
                    return false;
            }
        }

        public static class RenderStates
        {
            public static readonly RenderStateOverride[] Default = new RenderStateOverride[]
            {
                // Opaque
                RenderStateOverride.ZTest(ZTest.LEqual, 0),
                RenderStateOverride.ZWrite(ZWrite.On, 0),
                RenderStateOverride.Blend(Blend.One, Blend.Zero, 0),
                RenderStateOverride.Cull(Cull.Back, 0),

                // Alpha Test
                RenderStateOverride.Cull(Cull.Off, 1, new IField[] {DefaultFields.DoubleSided}),

                // Transparent
                RenderStateOverride.ZWrite(ZWrite.Off, 1, new IField[] { DefaultFields.SurfaceTransparent }),

                // Blend Mode
                RenderStateOverride.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendAlpha }),
                RenderStateOverride.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendPremultiply }),
                RenderStateOverride.Blend(Blend.One, Blend.One, Blend.One, Blend.One, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendAdd }),
                RenderStateOverride.Blend(Blend.DstColor, Blend.Zero, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendAdd }),
            };

            public static readonly RenderStateOverride[] ShadowCasterMeta = new RenderStateOverride[]
            {
                // Opaque
                RenderStateOverride.ZTest(ZTest.LEqual, 0),
                RenderStateOverride.ZWrite(ZWrite.On, 0),
                RenderStateOverride.Blend(Blend.One, Blend.Zero, 0),
                RenderStateOverride.Cull(Cull.Back, 0),

                // Alpha Test
                RenderStateOverride.Cull(Cull.Off, 1, new IField[] {DefaultFields.DoubleSided}),

                // Blend Mode
                RenderStateOverride.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendAlpha }),
                RenderStateOverride.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendPremultiply }),
                RenderStateOverride.Blend(Blend.One, Blend.One, Blend.One, Blend.One, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendAdd }),
                RenderStateOverride.Blend(Blend.DstColor, Blend.Zero, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendAdd }),
            };

            public static readonly RenderStateOverride[] DepthOnly = new RenderStateOverride[]
            {
                // Opaque
                RenderStateOverride.ZTest(ZTest.LEqual, 0),
                RenderStateOverride.ZWrite(ZWrite.On, 0),
                RenderStateOverride.Blend(Blend.One, Blend.Zero, 0),
                RenderStateOverride.Cull(Cull.Back, 0),
                RenderStateOverride.ColorMask("0", 0),

                // Alpha Test
                RenderStateOverride.Cull(Cull.Off, 1, new IField[] {DefaultFields.DoubleSided}),

                // Blend Mode
                RenderStateOverride.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendAlpha }),
                RenderStateOverride.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendPremultiply }),
                RenderStateOverride.Blend(Blend.One, Blend.One, Blend.One, Blend.One, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendAdd }),
                RenderStateOverride.Blend(Blend.DstColor, Blend.Zero, 1, new IField[] { DefaultFields.SurfaceTransparent, DefaultFields.BlendAdd }),
            };
        }
    }
}
