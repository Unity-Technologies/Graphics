namespace UnityEditor.Rendering.Universal.Converters
{
    internal sealed class BuiltInToURP2DConverterContainer : RenderPipelineConverterContainer
    {
        public override string name => "Convert Built-in to 2D (URP)";
        public override string info => "The 2D Render Pipeline Converter can convert scenes, prefabs and other assets created with the built-in renderer for use with the current version of 2D URP";
    }
}
