using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.VFX.HDRP
{
    [VFXInfo(experimental = true)]
    class VFXDecalHDRPOutput : VFXAbstractParticleHDRPOutput
    {
        public override string name { get { return "Output Particle HDRP Decal"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleHDRPDecal"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleHexahedronOutput; } }

        public override void OnEnable()
        {
            base.OnEnable();
            blendMode = BlendMode.Opaque;
            useNormalScale = false;
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                if (colorMode != ColorMode.None)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);
            }
        }

        public class WithoutMaskMapProperties
        {
            [Range(0, 1), Tooltip("Controls the metallic of the decal.")]
            public float metallic = 0.0f;
            [Range(0, 1), Tooltip("Controls the ambient occlusion of the decal.")]
            public float ambientOcclusion = 1.0f;
            [Range(0, 1), Tooltip("Controls the smoothness of the decal.")]
            public float smoothness = 0.5f;
        }

        public class WithMaskMapProperties
        {
            [Range(0, 1), Tooltip("Controls the scale factor for the particle’s metallic.")]
            public float metallic = 1.0f;
            [Range(0, 1), Tooltip("Controls the scale factor for the particle’s ambient occlusion.")]
            public float ambientOcclusion = 1.0f;
            [Range(0, 1), Tooltip("Controls the scale factor for the particle’s smoothness.")]
            public float smoothness = 1.0f;
        }

        protected IEnumerable<VFXPropertyWithValue> materialProperties
        {
            get
            {
                if(affectMetal)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float),
                    "metallic",
                    new TooltipAttribute(useMaskMap ?
                        "Controls the scale factor for the particle’s metallic." :
                        "Controls the metallic of the decal."),
                    new RangeAttribute(0,1)), 0.0f);

                if(affectAmbientOcclusion)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float),
                        "ambientOcclusion",
                        new TooltipAttribute(useMaskMap ?
                            "Controls the scale factor for the particle’s ambient occlusion." :
                            "Controls the ambient occlusion of the decal."),
                        new RangeAttribute(0,1)), 1.0f);

                if(affectSmoothness)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float),
                        "smoothness",
                        new TooltipAttribute(useMaskMap ?
                            "Controls the scale factor for the particle’s smoothness." :
                            "Controls the smoothness of the decal."),
                        new RangeAttribute(0,1)), 0.5f);
            }
        }



        public enum BlendSource
        {
            BaseColorMapAlpha,
            MaskMapBlue,
        }
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the source this Material uses as opacity for its Normal Map.")]
        BlendSource normalOpacityChannel = BlendSource.BaseColorMapAlpha;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the source this Material uses as opacity for its Mask Map.")]
        BlendSource maskOpacityChannel = BlendSource.BaseColorMapAlpha;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip("When enabled, this decal uses its base color. When disabled, the decal has no base color effect.")]
        private bool affectBaseColor = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip("When enabled, this decal uses the metallic channel of its Mask Map. When disabled, the decal has no metallic effect.")]
        private bool affectMetal = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip("When enabled, this decal uses the ambient occlusion channel of its Mask Map. When disabled, the decal has no ambient occlusion effect.")]
        private bool affectAmbientOcclusion = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip("When enabled, this decal uses the smoothness channel of its Mask Map. When disabled, the decal has no smoothness effect.")]
        private bool affectSmoothness = true;

        private bool enableDecalLayers => HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportDecals
                                                  && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportDecalLayers;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip("Specifies the layer mask of the decal.")]
        private DecalLayerEnum decalLayer = DecalLayerEnum.LightLayerDefault;

        public class FadeFactorProperty
        {
            [Range(0, 1), Tooltip("Fade Factor.")]
            public float fadeFactor = 1.0f;
        }

        public class AngleFadeProperty
        {
            [Tooltip("Angle Fade. Between 0 and 180.")] //TODO : create range attribute?
            public Vector2 angleFade = new Vector2(0.0f, 180.0f);
        }
        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = Enumerable.Empty<VFXPropertyWithValue>();

                properties = properties.Concat(PropertiesFromType("FadeFactorProperty"));
                if(enableDecalLayers)
                    properties = properties.Concat(PropertiesFromType("AngleFadeProperty"));

                properties = properties.Concat(base.inputProperties);
                properties =
                    properties.Concat(materialProperties);
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (GetOrRefreshShaderGraphObject() == null)
            {
                yield return slotExpressions.First(o => o.name == "fadeFactor");
                if(affectMetal)
                    yield return slotExpressions.First(o => o.name == "metallic");
                if(affectAmbientOcclusion)
                    yield return slotExpressions.First(o => o.name == "ambientOcclusion");
                if(affectSmoothness)
                    yield return slotExpressions.First(o => o.name == "smoothness");

                if(enableDecalLayers)
                {
                    var angleFadeExp = slotExpressions.First(o => o.name == "angleFade");
                    yield return new VFXNamedExpression(AngleFadeSimplification(angleFadeExp.exp), "angleFade");
                    yield return new VFXNamedExpression(VFXValue.Constant((uint)decalLayer), "decalLayerMask");
                }
            }
        }

        VFXExpression AngleFadeSimplification(VFXExpression angleFadeExp)
        {
            angleFadeExp = angleFadeExp / VFXValue.Constant(new Vector2(180.0f,180.0f));
            var angleStart = new VFXExpressionExtractComponent(angleFadeExp, 0);
            var angleEnd = new VFXExpressionExtractComponent(angleFadeExp, 1);
            var range = new VFXExpressionMax(VFXValue.Constant(0.0001f), angleEnd - angleStart);
            var simplifiedAngleFade = new VFXExpressionCombine(
                VFXValue.Constant(1.0f) - (VFXValue.Constant(0.25f) - angleStart) / range,
                VFXValue.Constant(-0.25f)/ range);
            return simplifiedAngleFade;
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
                yield return "cullMode";
                yield return "blendMode";
                yield return "useAlphaClipping";
                yield return "doubleSided";
                yield return "shaderGraph";
                yield return "zTestMode";
                yield return "zWriteMode";
                yield return "castShadows";
                yield return "materialType";

                if (!enableDecalLayers)
                    yield return "decalLayer";
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var def in base.additionalDefines)
                    yield return def;
                if (maskOpacityChannel == BlendSource.BaseColorMapAlpha)
                    yield return "VFX_MASK_BLEND_BASE_COLOR_ALPHA";
                else
                    yield return "VFX_MASK_BLEND_MASK_BLUE";

                if (normalOpacityChannel == BlendSource.BaseColorMapAlpha)
                    yield return "VFX_NORMAL_BLEND_BASE_COLOR_ALPHA";
                else
                    yield return "VFX_NORMAL_BLEND_MASK_BLUE";

                if (affectMetal)
                    yield return "AFFECT_METALLIC";
                if (affectAmbientOcclusion)
                    yield return "AFFECT_AMBIENT_OCCLUSION";
                if (affectSmoothness)
                    yield return "AFFECT_SMOOTHNESS";

                if(enableDecalLayers)
                {
                    yield return "VFX_ENABLE_DECAL_LAYERS";
                }
            }
        }
        protected override void WriteBlendMode(VFXShaderWriter writer)  //TODO : Not sure we need to do it here : different for DBuffer and ForwardEmissive pass
        {
            // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
            for (int i = 0; i < 3; i++)
            {
                writer.WriteLineFormat("Blend {0} SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha", i);
            }
            writer.WriteLine("Blend 3 Zero OneMinusSrcColor");
        }

        protected VFXShaderWriter GetDecalMaskColor(int maskIndex)
        {
            var rs = new VFXShaderWriter();
            var maskString = "";
            switch (maskIndex)
            {
                case 0 :
                    rs.Write(affectBaseColor ? "RBGA" : "0"); break;
                case 1 :
                    rs.Write(useNormalMap ? "RGBA" : "0"); break;
                case 2:
                {
                    ColorWriteMask mask2 = 0;
                    if (affectMetal)
                    {
                        maskString += "R";
                    }

                    if (affectAmbientOcclusion)
                    {
                        maskString += "G";
                    }

                    if (affectSmoothness)
                    {
                        maskString += "BA";
                    }

                    if (String.IsNullOrEmpty(maskString))
                        maskString = "0";
                    rs.Write(maskString);
                    break;
                }
                case 3 :
                    ColorWriteMask mask3 = 0;
                    if (affectMetal)
                    {
                        maskString += "R";
                    }

                    if (affectAmbientOcclusion)
                    {
                        maskString += "G";
                    }
                    if (String.IsNullOrEmpty(maskString))
                        maskString = "0";
                    rs.Write(maskString);
                    break;
            }

            return rs;
        }


        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                foreach (var rep in base.additionalReplacements)
                    yield return rep;

                for (int i = 0; i < 4; i++)
                {
                    yield return new KeyValuePair<string, VFXShaderWriter>("${VFXDecalColorMask" + i + "}", GetDecalMaskColor(i));
                }
            }
        }
    }
}
