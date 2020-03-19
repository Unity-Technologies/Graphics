using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalSubShaders
    {
        const string kPipelineTag = "UniversalPipeline";

        public static SubShaderDescriptor PBR = new SubShaderDescriptor()
        {
            pipelineTag = kPipelineTag,
            generatesPreview = true,
            passes = new PassCollection
            {
                { UniversalPasses.Forward },
                { UniversalPasses.GBuffer },
                { UniversalPasses.ShadowCaster },
                { UniversalPasses.DepthOnly },
                { UniversalPasses.Meta },
                { UniversalPasses._2D },
            },
            customEditorOverride = @"CustomEditor ""UnityEditor.ShaderGraph.PBRMasterGUI""",
        };

        public static SubShaderDescriptor DOTSPBR
        {
            get
            {
                var forward = UniversalPasses.Forward;
                var gbuffer = UniversalPasses.GBuffer;
                var shadowCaster = UniversalPasses.ShadowCaster;
                var depthOnly = UniversalPasses.DepthOnly;
                var meta = UniversalPasses.Meta;
                var _2d = UniversalPasses._2D;

                forward.pragmas = UniversalPragmas.DOTSForward;
                gbuffer.pragmas = UniversalPragmas.DOTSGBuffer;
                shadowCaster.pragmas = UniversalPragmas.DOTSInstanced;
                depthOnly.pragmas = UniversalPragmas.DOTSInstanced;
                meta.pragmas = UniversalPragmas.DOTSDefault;
                _2d.pragmas = UniversalPragmas.DOTSDefault;
                
                return new SubShaderDescriptor()
                {
                    pipelineTag = kPipelineTag,
                    generatesPreview = true,
                    passes = new PassCollection
                    {
                        { forward },
                        { gbuffer },
                        { shadowCaster },
                        { depthOnly },
                        { meta },
                        { _2d },
                    },
                    customEditorOverride = @"CustomEditor ""UnityEditor.ShaderGraph.PBRMasterGUI""",
                };
            }
        }
        
        public static SubShaderDescriptor Unlit = new SubShaderDescriptor()
        {
            pipelineTag = kPipelineTag,
            generatesPreview = true,
            passes = new PassCollection
            {
                { UniversalPasses._2DUnlit }, // This pass use legacy lightMode tag and is picked up by 2D renderer.
                { UniversalPasses.UniversalUnlit }, // This pass is picked up by the forward and deferred renderers.
                { UniversalPasses.ShadowCaster },
                { UniversalPasses.DepthOnly },
            },
        };

        public static SubShaderDescriptor DOTSUnlit
        {
            get
            {
                var _2DUnlit = UniversalPasses._2DUnlit;
                var universalUnlit = UniversalPasses.UniversalUnlit;
                var shadowCaster = UniversalPasses.ShadowCaster;
                var depthOnly = UniversalPasses.DepthOnly;

                _2DUnlit.pragmas = UniversalPragmas.DOTSForward;
                universalUnlit.pragmas = UniversalPragmas.DOTSForward;
                shadowCaster.pragmas = UniversalPragmas.DOTSInstanced;
                depthOnly.pragmas = UniversalPragmas.DOTSInstanced;
                
                return new SubShaderDescriptor()
                {
                    pipelineTag = kPipelineTag,
                    generatesPreview = true,
                    passes = new PassCollection
                    {
                        { _2DUnlit }, // This pass use legacy lightMode tag and is picked up by 2D renderer.
                        { universalUnlit }, // This pass is picked up by the forward and deferred renderers.
                        { shadowCaster },
                        { depthOnly },
                    },
                };
            }
        }

        public static SubShaderDescriptor SpriteLit = new SubShaderDescriptor()
        {
            pipelineTag = kPipelineTag,
            generatesPreview = true,
            passes = new PassCollection
            {
                { UniversalPasses.SpriteLit },
                { UniversalPasses.SpriteNormal },
                { UniversalPasses.SpriteForward },
            },
        };

        public static SubShaderDescriptor SpriteUnlit = new SubShaderDescriptor()
        {
            pipelineTag = kPipelineTag,
            generatesPreview = true,
            passes = new PassCollection
            {
                { UniversalPasses.SpriteUnlit },
            },
        };
    }
}
