using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.SurfaceOptionUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.LitSurfaceInputsUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.RefractionUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class LitSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        HDLitData litData;

        class Styles
        {
            public static GUIContent enableClearCoat = new GUIContent("Clear Coat", "Enable Clear Coat");
        }

        public LitSurfaceOptionPropertyBlock(HDLitData litData) : base(SurfaceOptionPropertyBlock.Features.Lit)
            => this.litData = litData;

        protected override void CreatePropertyGUI()
        {
            // Lit specific properties:
            AddProperty(materialIDText, () => litData.materialTypeMask, (newValue) => litData.materialTypeMask = newValue);
            AddProperty(rayTracingText, () => litData.rayTracing, (newValue) => litData.rayTracing = newValue);

            base.CreatePropertyGUI();

            if (litData.HasMaterialType(~HDLitData.MaterialTypeMask.ColoredTranslucent))
                AddProperty(Styles.enableClearCoat, () => litData.clearCoat, (newValue) => litData.clearCoat = newValue);

            if (litData.HasMaterialType(HDLitData.MaterialTypeMask.SubsurfaceScattering))
            {
                AddProperty(transmissionEnableText, () => litData.sssTransmission, (newValue) => litData.sssTransmission = newValue);
            }
            if (systemData.surfaceType == SurfaceType.Transparent)
            {
                AddProperty(refractionModelText, () => litData.refractionModel, (newValue) => litData.refractionModel = newValue);
                if (litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None)
                {
                    if (systemData.blendingMode != BlendingMode.Alpha)
                        AddHelpBox(RefractionUIBlock.Styles.refractionBlendModeWarning, MessageType.Warning);
                    if (systemData.renderQueueType == HDRenderQueue.RenderQueueType.PreRefraction)
                        AddHelpBox(RefractionUIBlock.Styles.refractionRenderingPassWarning, MessageType.Warning);
                }
            }
            if (litData.HasMaterialType(HDLitData.MaterialTypeMask.SpecularColor))
            {
                AddProperty(energyConservingSpecularColorText, () => litData.energyConservingSpecular, (newValue) => litData.energyConservingSpecular = newValue);
            }
        }
    }
}
