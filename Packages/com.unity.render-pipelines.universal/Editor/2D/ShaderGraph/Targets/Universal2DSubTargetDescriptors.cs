using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    internal static class Universal2DSubTargetDescriptors
    {
        internal static class Keywords
        {
            public static readonly KeywordDescriptor Sort3DAs2DCompatible = new KeywordDescriptor()
            {
                displayName = "Sort 3D As 2D Compatible",
                referenceName = "_ENABLE_SORT_3D_AS_2D_COMPATIBLE",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.All,
            };
        }

        internal static class RenderStateCollections
        {
            public static readonly RenderStateCollection Sort3DAs2DCompatible = new RenderStateCollection()
            {
                { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendAlpha, true)},
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.Cull(Cull.Back) },
                { RenderState.Stencil(new StencilDescriptor() {
                    Ref  = "128",
                    Comp = "Always",
                    Pass = "Replace",
                })},
            };
        }
    }
}
