// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.DecalSurfaceOptionsUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class DecalPropertyBlock : SubTargetPropertyBlock
    {
        DecalData decalData;

        protected override string title => "Surface Options";
        protected override int foldoutIndex => 4;

        public DecalPropertyBlock(DecalData decalData) => this.decalData = decalData;

        protected override void CreatePropertyGUI()
        {
            AddProperty(affectAlbedoText, decalData.affectsAlbedoProp);
            AddProperty(affectNormalText, decalData.affectsNormalProp);
            AddProperty(affectMetalText, decalData.affectsMetalProp);
            AddProperty(affectAmbientOcclusionText, decalData.affectsAOProp);
            AddProperty(affectSmoothnessText, decalData.affectsSmoothnessProp);
            AddProperty(affectEmissionText, decalData.affectsEmissionProp);

            AddProperty(supportLodCrossFadeText, () => decalData.supportLodCrossFade, (newValue) => decalData.supportLodCrossFade = newValue);
        }
    }
}
