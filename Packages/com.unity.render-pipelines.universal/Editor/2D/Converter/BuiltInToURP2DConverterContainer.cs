namespace UnityEditor.Rendering.Universal
{
    internal sealed class BuiltInToURP2DConverterContainer : RenderPipelineConverterContainer
    {
        public override string name => "2D Converters/Built-in to 2D URP Material Upgrader";
        public override string info => "Converter performs the following tasks:\n* Converts project elements from the Built-in Render Pipeline to 2D URP.";
    }
}
