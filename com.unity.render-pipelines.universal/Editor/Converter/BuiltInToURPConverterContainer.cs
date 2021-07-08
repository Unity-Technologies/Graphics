namespace UnityEditor.Rendering.Universal.Converters
{
    internal sealed class BuiltInToURPConverterContainer : RenderPipelineConverterContainer
    {
        public override string name => "Built-in to URP";
        public override string info => "The Render Pipeline Converter performs the following tasks:\n* Converts project elements from the Built-in Render Pipeline to URP.\n* Upgrades assets from earlier URP versions to the current URP version.";
    }
}
