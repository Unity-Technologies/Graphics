using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public enum EmissiveIntensityUnit
    {
        Luminance,
        EV100,
    }

    [Title("Input", "High Definition Render Pipeline", "Emission Node")]
    class EmissionNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public EmissionNode()
        {
            name = "Emission Node";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            // TODO: write the doc
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Emission-Node"; }
        }

        [SerializeField]
        EmissiveIntensityUnit _intensityUnit;

        [EnumControl()]
        EmissiveIntensityUnit intensityUnit
        {
            get { return _intensityUnit; }
            set
            {
                _intensityUnit = value;
                Dirty(ModificationScope.Node);
            }
        }

        const int kEmissionOutputSlotId = 0;
        const int kEmissionColorInputSlotId = 1;
        const int kEmissionIntensityInputSlotId = 2;
        const int kEmissionExposureWeightInputSlotId = 3;
        const string kEmissionOutputSlotName = "Output";
        const string kEmissionColorInputSlotName = "Color";
        const string kEmissionExpositionWeightInputSlotName = "ExpositionWeight";
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

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            var sb = new ShaderStringBuilder();

            var colorValue = GetSlotValue(kEmissionColorInputSlotId, generationMode);
            var intensityValue = GetSlotValue(kEmissionIntensityInputSlotId, generationMode);
            var exposureWeightValue = GetSlotValue(kEmissionExposureWeightInputSlotId, generationMode);
            
            if (intensityUnit == EmissiveIntensityUnit.EV100)
                intensityValue = "ConvertEvToLuminance(" + intensityValue + ")";
            
            sb.AppendLine(@"{0}3 {1} = {2}({3}, {4}, {5}, {6});",
                precision,
                GetVariableNameForNode(),
                GetFunctionName(),
                colorValue,
                intensityValue,
                exposureWeightValue,
                "GetInverseCurrentExposureMultiplier()"
            );

            visitor.AddShaderChunk(sb.ToString(), true);
        }

        string GetFunctionName()
        {
            return "Unity_HDRP_GetEmissionHDRColor";
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("{1}3 {0}({1}3 ldrColor, {2} luminanceIntensity, {2} exposureWeight, {2} inverseCurrentExposureMultiplier)",
                        GetFunctionName(),
                        precision,
                        intensitySlot.concreteValueType.ToString(precision));
                    using (s.BlockScope())
                    {
                        s.AppendLine("{0}3 hdrColor = ldrColor * luminanceIntensity;", precision);
                        s.AppendNewLine();
                        s.AppendLine("// Inverse pre-expose using _EmissiveExposureWeight weight");
                        s.AppendLine("hdrColor = lerp(hdrColor * inverseCurrentExposureMultiplier, hdrColor, exposureWeight);", precision);
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
                name = GetVariableNameForNode(),
                vector4Value = outputColor
            });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }
    }
}
