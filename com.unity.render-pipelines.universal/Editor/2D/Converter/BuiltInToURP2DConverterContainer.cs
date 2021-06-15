namespace UnityEditor.Rendering.Universal.Converters
{
    internal sealed class BuiltInToURP2DConverterContainer : RenderPipelineConverterContainer
    {
        public override string name => "Convert Built-in to 2D (URP)";
        public override string info => "Converter performs the following tasks:\n* Converts project elements from the Built-in Render Pipeline to 2D URP.";
    }
}
