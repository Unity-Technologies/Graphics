namespace UnityEditor.Rendering.Universal.Converters
{
    internal sealed class UpgradeURP2DAssetsContainer : RenderPipelineConverterContainer
    {
        public override string name => "Upgrade 2D (URP) Assets";
        public override string info => "Converter performs the following tasks:\n* Upgrades assets from earlier 2D URP versions to the current 2D URP version.";
    }
}
