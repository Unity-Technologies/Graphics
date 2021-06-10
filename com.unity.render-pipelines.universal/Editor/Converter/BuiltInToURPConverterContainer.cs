namespace UnityEditor.Rendering.Universal.Converters
{
    internal sealed class BuiltInToURPConverterContainer : RenderPipelineConverterContainer
    {
        public override string name => "Built-in to URP";
        public override string info => "The Render Pipeline Convertor is meant to help convert projects from built-in to URP or upgrade URP assets.";
    }
}
