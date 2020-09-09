using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    enum EmissiveIntensityUnit
    {
        Nits,
        EV100,
    }

    [SRPFilter(typeof(HDRenderPipeline))]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.EmissionNode")]
    [Title("Utility", "High Definition Render Pipeline", "Emission Node")]
    class EmissionNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public EmissionNode()
        {
            name = "Emission Node";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("SGNode-Emission");

        [SerializeField]
        EmissiveIntensityUnit _intensityUnit;

        [EnumControl]
        EmissiveIntensityUnit intensityUnit
        {
            get { return _intensityUnit; }
            set
            {
                _intensityUnit = value;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        bool        m_NormalizeColor;

        [ToggleControl("Normalize Color")]
        ToggleData  normalizeColor
        {
            get { return new ToggleData(m_NormalizeColor); }
            set
            {
                m_NormalizeColor = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        const int kEmissionOutputSlotId = 0;
        const int kEmissionColorInputSlotId = 1;
        const int kEmissionIntensityInputSlotId = 2;
        const int kEmissionExposureWeightInputSlotId = 3;
        const string kEmissionOutputSlotName = "Output";
        const string kEmissionColorInputSlotName = "Color";
        const string kEmissionExpositionWeightInputSlotName = "Exposure Weight";
        const string kEmissionIntensityInputSlotName = "Intensity";

        public override bool hasPreview { get { return false; } }

        ColorRGBMaterialSlot    ldrColorSlot;
        Vector1MaterialSlot     intensitySlot;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input slots:
            ldrColorSlot = new ColorRGBMaterialSlot(kEmissionColorInputSlotId, kEmissionColorInputSlotName, kEmissionColorInputSlotName, SlotType.Input, Color.black, ColorMode.Default);
            intensitySlot = new Vector1MaterialSlot(kEmissionIntensityInputSlotId, kEmissionIntensityInputSlotName, kEmissionIntensityInputSlotName, SlotType.Input, 1);
            AddSlot(ldrColorSlot);
            AddSlot(intensitySlot);
            AddSlot(new Vector1MaterialSlot(kEmissionExposureWeightInputSlotId, kEmissionExpositionWeightInputSlotName, kEmissionExpositionWeightInputSlotName, SlotType.Input, 1));

            // Output slot:kEmissionOutputSlotName
            AddSlot(new ColorRGBMaterialSlot(kEmissionOutputSlotId, kEmissionOutputSlotName, kEmissionOutputSlotName , SlotType.Output, Color.black, ColorMode.HDR));

            RemoveSlotsNameNotMatching(new[] {
                kEmissionOutputSlotId, kEmissionColorInputSlotId,
                kEmissionIntensityInputSlotId, kEmissionExposureWeightInputSlotId
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var colorValue = GetSlotValue(kEmissionColorInputSlotId, generationMode);
            var intensityValue = GetSlotValue(kEmissionIntensityInputSlotId, generationMode);
            var exposureWeightValue = GetSlotValue(kEmissionExposureWeightInputSlotId, generationMode);
            var outputValue = GetSlotValue(kEmissionOutputSlotId, generationMode);

            if (intensityUnit == EmissiveIntensityUnit.EV100)
                intensityValue = "ConvertEvToLuminance(" + intensityValue + ")";

            sb.AppendLine("#ifdef SHADERGRAPH_PREVIEW");
            sb.AppendLine($"$precision inverseExposureMultiplier = 1.0;");
            sb.AppendLine("#else");
            sb.AppendLine($"$precision inverseExposureMultiplier = GetInverseCurrentExposureMultiplier();");
            sb.AppendLine("#endif");

            sb.AppendLine(@"$precision3 {0} = {1}({2}.xyz, {3}, {4}, inverseExposureMultiplier);",
                outputValue,
                GetFunctionName(),
                colorValue,
                intensityValue,
                exposureWeightValue
            );
        }

        string GetFunctionName()
        {
            return $"Unity_HDRP_GetEmissionHDRColor_{concretePrecision.ToShaderString()}";
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    // We may need ConvertEvToLuminance() so we include CommonLighting.hlsl
                    s.AppendLine("#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl\"");

                    s.AppendLine("$precision3 {0}($precision3 ldrColor, {1} luminanceIntensity, {1} exposureWeight, {1} inverseCurrentExposureMultiplier)",
                        GetFunctionName(),
                        intensitySlot.concreteValueType.ToShaderString());
                    using (s.BlockScope())
                    {
                        if (normalizeColor.isOn)
                        {
                            s.AppendLine("ldrColor = ldrColor * rcp(max(Luminance(ldrColor), 1e-6));");
                        }
                        s.AppendLine("$precision3 hdrColor = ldrColor * luminanceIntensity;");
                        s.AppendNewLine();
                        s.AppendLine("// Inverse pre-expose using _EmissiveExposureWeight weight");
                        s.AppendLine("hdrColor = lerp(hdrColor * inverseCurrentExposureMultiplier, hdrColor, exposureWeight);");
                        s.AppendLine("return hdrColor;");
                    }
                });
        }

        Vector3 GetHDREmissionColor(Vector3 ldrColor, float intensity)
        {
            float multiplier = intensity;

            if (intensityUnit == EmissiveIntensityUnit.EV100)
                multiplier = LightUtils.ConvertEvToLuminance(intensity);

            return ldrColor * intensity;
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            Vector3 outputColor = GetHDREmissionColor(ldrColorSlot.value, intensitySlot.value);

            properties.Add(new PreviewProperty(PropertyType.Vector3)
            {
                name = GetVariableNameForSlot(kEmissionColorInputSlotId),
                vector4Value = outputColor
            });
        }
    }
}
