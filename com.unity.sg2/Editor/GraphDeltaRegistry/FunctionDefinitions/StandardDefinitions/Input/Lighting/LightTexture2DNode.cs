using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class LightTexture2DNode : IStandardNode
    {
        public static string Name => "LightTexture2D";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "LightTex0",
                    "Out = UnityBuildTexture2DStructNoScale(_ShapeLightTexture0);",
                    new ParameterDescriptor[]
                    {//TODO: need a light texture type, or a light tetexture REF
                        new ParameterDescriptor("Out", TYPE.Texture2D, Usage.Out)
                    }
                ),
                new (
                    "LightTex1",
                    "Out = UnityBuildTexture2DStructNoScale(_ShapeLightTexture1);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Out", TYPE.Texture2D, Usage.Out)
                    }
                ),
                new (
                    "LightTex2",
                    "Out = UnityBuildTexture2DStructNoScale(_ShapeLightTexture2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Out", TYPE.Texture2D, Usage.Out)
                    }
                ),
                new (
                    "LightTex3",
                    "Out = UnityBuildTexture2DStructNoScale(_ShapeLightTexture3);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Out", TYPE.Texture2D, Usage.Out)
                    }
                ),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Light Texture 2D",
            tooltip: "A 2D Light texture asset.",
            category: "Utility/Input/Lighting",
            synonyms: new string[0],
            hasPreview: false,
            selectableFunctions: new()
            {
                { "LightTex0", "LightTex0" },
                { "LightTex1", "LightTex1" },
                { "LightTex2", "LightTex2" },
                { "LightTex3", "LightTex3" }
        },
            functionSelectorLabel: "Blend Style",
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: "Out",
                    tooltip: "selected 2D light texture"
                )
            }
        );
    }
}
