using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDPragmas
    {
        public static PragmaCollection Basic = new PragmaCollection
        {
            { Pragma.Target(4.5) },
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

        public static PragmaCollection InstancedRenderingPlayer = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
        };

        public static PragmaCollection InstancedRenderingPlayerEditorSync = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            { Pragma.EditorSyncCompilation },
        };

        public static PragmaCollection DotsInstanced = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, true),
                new FieldCondition(HDFields.DotsProperties, true),
            } },
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, true),
                new FieldCondition(HDFields.DotsProperties, false),
            } },
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, false),
                new FieldCondition(HDFields.DotsProperties, true),
            } },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, true),
                new FieldCondition(HDFields.DotsProperties, true),
            } },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, true),
                new FieldCondition(HDFields.DotsProperties, false),
            } },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, false),
                new FieldCondition(HDFields.DotsProperties, true),
            } },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, false),
                new FieldCondition(HDFields.DotsProperties, false),
            } },
        };

        public static PragmaCollection DotsInstancedEditorSync = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.EditorSyncCompilation },
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, true),
                new FieldCondition(HDFields.DotsProperties, true),
            } },
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, true),
                new FieldCondition(HDFields.DotsProperties, false),
            } },
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, false),
                new FieldCondition(HDFields.DotsProperties, true),
            } },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, true),
                new FieldCondition(HDFields.DotsProperties, true),
            } },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, true),
                new FieldCondition(HDFields.DotsProperties, false),
            } },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, false),
                new FieldCondition(HDFields.DotsProperties, true),
            } },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, false),
                new FieldCondition(HDFields.DotsProperties, false),
            } },
        };

        public static PragmaCollection RaytracingBasic = new PragmaCollection
        {
            { Pragma.Target(4.5) },
            { Pragma.Raytracing("test") },
            { Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.PS4, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch}) },
        };

        public static PragmaCollection RaytracingInstanced = new PragmaCollection
        {
            { Pragma.Target(4.5) },
            { Pragma.Raytracing("test") },
            { Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.PS4, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch}) },
            { Pragma.MultiCompileInstancing },
        };
    }
}
