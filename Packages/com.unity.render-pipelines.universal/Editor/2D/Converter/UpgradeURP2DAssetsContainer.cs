namespace UnityEditor.Rendering.Universal
{
    internal sealed class UpgradeURP2DAssetsContainer : RenderPipelineConverterContainer
    {
        public override string name => "2D Converters/Upgrade Deprecated 2D Features";
        public override string info => "Converter performs the following tasks:\n* Upgrades assets from earlier 2D URP versions to the current 2D URP version.";
    }
}
