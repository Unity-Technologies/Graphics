namespace UnityEditor.Rendering.Universal.Converters
{
    internal sealed class BuiltInToURP2DConverterContainer : RenderPipelineConverterContainer
    {
        public override string name => "Convert Built-in to 2D (URP)";
        public override string info => "This conversion wizard can convert scenes, prefabs and other assets created with the built-in renderer for use with the 2D URP";
    }
}
