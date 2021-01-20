using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

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

        public LitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features features, HDLitData litData) : base(features)
            => this.litData = litData;

        protected override void CreatePropertyGUI()
        {
            // Lit specific properties:
            AddProperty(materialIDText, "HDLitData.materialType", () => litData.materialType, (newValue) => litData.materialType = newValue);
            AddProperty(rayTracingText, "HDLitData.rayTracing", () => litData.rayTracing, (newValue) => litData.rayTracing = newValue);

            base.CreatePropertyGUI();

            AddProperty(Styles.enableClearCoat, "HDLitData.clearCoat", () => litData.clearCoat, (newValue) => litData.clearCoat = newValue);
            if (litData.materialType == HDLitData.MaterialType.SubsurfaceScattering)
            {
                AddProperty(transmissionEnableText, "HDLitData.sssTransmission", () => litData.sssTransmission, (newValue) => litData.sssTransmission = newValue);
            }
            if (systemData.surfaceType == SurfaceType.Transparent)
            {
                AddProperty(refractionModelText, "HDLitData.refractionModel", () => litData.refractionModel, (newValue) => litData.refractionModel = newValue);
                if (litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None)
                {
                    if (systemData.blendMode != BlendMode.Alpha)
                        AddHelpBox(RefractionUIBlock.Styles.refractionBlendModeWarning, MessageType.Warning);
                    if (systemData.renderQueueType == HDRenderQueue.RenderQueueType.PreRefraction)
                        AddHelpBox(RefractionUIBlock.Styles.refractionRenderingPassWarning, MessageType.Warning);
                }
            }
            if (litData.materialType == HDLitData.MaterialType.SpecularColor)
            {
                AddProperty(energyConservingSpecularColorText, "HDLitData.energyConservingSpecular", () => litData.energyConservingSpecular, (newValue) => litData.energyConservingSpecular = newValue);
            }
        }
    }
}
