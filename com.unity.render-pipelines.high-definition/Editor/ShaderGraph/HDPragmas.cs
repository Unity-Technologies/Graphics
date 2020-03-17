using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDPragmas
    {
        public static PragmaCollection Basic = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.Vertex("Vert") },
            { Pragma.Fragment("Frag") },
            { Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.PS4, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch}) },
        };

        public static PragmaCollection Instanced = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
        };

        public static PragmaCollection InstancedEditorSync = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.EditorSyncCompilation },
        };

        public static PragmaCollection InstancedRenderingLayer = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
        };

        public static PragmaCollection InstancedRenderingLayerEditorSync = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            { Pragma.EditorSyncCompilation },
        };

        public static PragmaCollection DotsInstancedInV1AndV2 = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            // Hybrid Renderer V2 requires a completely different set of pragmas from Hybrid V1
            #if ENABLE_HYBRID_RENDERER_V2
            { Pragma.DOTSInstancing },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            #else
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition(HDFields.DotsInstancing, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition(HDFields.DotsProperties, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade),    new FieldCondition(HDFields.DotsInstancing, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade),    new FieldCondition(HDFields.DotsProperties, true) },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, false),
                new FieldCondition(HDFields.DotsProperties, false),
            } },
            #endif
        };

        public static PragmaCollection DotsInstancedInV1AndV2EditorSync = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.EditorSyncCompilation },
            // Hybrid Renderer V2 requires a completely different set of pragmas from Hybrid V1
            #if ENABLE_HYBRID_RENDERER_V2
            { Pragma.DOTSInstancing },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            #else
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition(HDFields.DotsInstancing, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition(HDFields.DotsProperties, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade),    new FieldCondition(HDFields.DotsInstancing, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade),    new FieldCondition(HDFields.DotsProperties, true) },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, false),
                new FieldCondition(HDFields.DotsProperties, false),
            } },
            #endif
        };

        public static PragmaCollection DotsInstancedInV2Only = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            #if ENABLE_HYBRID_RENDERER_V2
            { Pragma.DOTSInstancing },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
            #endif
        };

        public static PragmaCollection DotsInstancedInV2OnlyEditorSync = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.EditorSyncCompilation },
            #if ENABLE_HYBRID_RENDERER_V2
            { Pragma.DOTSInstancing },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
            #endif
        };

        public static PragmaCollection DotsInstancedInV2OnlyRenderingLayer = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            #if ENABLE_HYBRID_RENDERER_V2
            { Pragma.DOTSInstancing },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
            #endif
        };

        public static PragmaCollection DotsInstancedInV2OnlyRenderingLayerEditorSync = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            { Pragma.EditorSyncCompilation },
            #if ENABLE_HYBRID_RENDERER_V2
            { Pragma.DOTSInstancing },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
            #endif
        };

        public static PragmaCollection RaytracingBasic = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target50) },
            { Pragma.Raytracing("test") },
            { Pragma.OnlyRenderers(new Platform[] {Platform.D3D11}) },
        };
    }
}
