namespace UnityEditor.Rendering.Universal
{
    internal sealed class BuiltInAndURP3DTo2DConverterContainer : RenderPipelineConverterContainer
    {
        public override string name => "2D Converters/Convert 3D Materials for 2D URP";
        public override string info => "Converter performs the following tasks:\n* Converts 3D project elements from the Built-in/URP Render Pipeline for use in 2D URP.";
    }
}
