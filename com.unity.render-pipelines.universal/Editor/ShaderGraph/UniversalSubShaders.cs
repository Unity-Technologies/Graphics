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
                { UniversalPasses.ShadowCaster },
                { UniversalPasses.DepthOnly },
                { UniversalPasses.Meta },
                { UniversalPasses._2D },
            },
            customEditorOverride = "ShaderGraph.PBRMasterGUI"
        };

        public static SubShaderDescriptor DOTSPBR
        {
            get
            {
                var forward = UniversalPasses.Forward;
                var shadowCaster = UniversalPasses.ShadowCaster;
                var depthOnly = UniversalPasses.DepthOnly;
                var meta = UniversalPasses.Meta;
                var _2d = UniversalPasses._2D;

                forward.pragmas = UniversalPragmas.DOTSForward;
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
                { UniversalPasses.Unlit },
                { UniversalPasses.ShadowCaster },
                { UniversalPasses.DepthOnly },
            },
        };

        public static SubShaderDescriptor DOTSUnlit
        {
            get
            {
                var unlit = UniversalPasses.Unlit;
                var shadowCaster = UniversalPasses.ShadowCaster;
                var depthOnly = UniversalPasses.DepthOnly;

                unlit.pragmas = UniversalPragmas.DOTSForward;
                shadowCaster.pragmas = UniversalPragmas.DOTSInstanced;
                depthOnly.pragmas = UniversalPragmas.DOTSInstanced;
                
                return new SubShaderDescriptor()
                {
                    pipelineTag = kPipelineTag,
                    generatesPreview = true,
                    passes = new PassCollection
                    {
                        { unlit },
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
