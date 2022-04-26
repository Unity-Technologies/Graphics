using UnityEngine.Rendering.Universal;


namespace UnityEditor.Rendering.Universal
{
    [URPHelpURL("features/rp-converter")]
    internal sealed class BuiltInToURPConverterContainer : RenderPipelineConverterContainer
    {
        public override string name => "Built-in to URP";
        public override string info => "This tool converts project elements from the Built-in Render Pipeline to URP.";
        public override int priority => -9000;
    }
}
