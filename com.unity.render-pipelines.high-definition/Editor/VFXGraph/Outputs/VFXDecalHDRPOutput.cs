using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

namespace UnityEditor.VFX.HDRP
{
    [VFXInfo(experimental = true)]
    class VFXDecalHDRPOutput : VFXAbstractParticleHDRPLitOutput
    {
        public override string name { get { return "Output Particle HDRP Decal"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleHDRPDecal"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleHexahedronOutput; } }

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

        public enum BlendSource
        {
            BaseColorMapAlpha,
            MaskMapBlue,
        }
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the source this Material uses as opacity for its Normal Map.")]
        BlendSource normalOpacityChannel = BlendSource.BaseColorMapAlpha;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the source this Material uses as opacity for its Mask Map.")]
        BlendSource maskOpacityChannel = BlendSource.BaseColorMapAlpha;

        private bool enableDecalLayers => HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportDecals
                                                  && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportDecalLayers;

        string[] decalLayerNames => HDRenderPipelineGlobalSettings.instance.decalLayerNames;
        


        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip("Specifies the layer mask of the decal.")]
        private DecalLayerEnum decalLayer = DecalLayerEnum.LightLayerDefault;

        public class NoMaskMapProperties
        {
            [Range(0, 1), Tooltip("Controls the metallic of the decal.")]
            public float metallic = 0.0f;
            [Range(0, 1), Tooltip("Controls the ambient occlusion of the decal.")]
            public float ambientOcclusion = 0.0f;
            [Range(0, 1), Tooltip("Controls the smoothness of the decal.")]
            public float smoothness = 0.0f;
        }

        public class FadingProperties
        {
            [Tooltip("Angle Fade. Between 0 and 180.")] //TODO : create range attribute?
            public Vector2 angleFade = Vector2.zero;
            [Range(0, 1), Tooltip("Fade Factor.")]
            public float fadeFactor = 0.0f;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                properties = properties.Concat(PropertiesFromType("FadingProperties"));
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (GetOrRefreshShaderGraphObject() == null)
            {
                //yield return slotExpressions.First(o => o.name == "startFade");
                yield return slotExpressions.First(o => o.name == "fadeFactor");
                if(enableDecalLayers)
                {
                    var angleFadeExp = slotExpressions.First(o => o.name == "angleFade");
                    yield return new VFXNamedExpression(AngleFadeSimplification(angleFadeExp.exp), "angleFade");
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
                {
                    yield return "VFX_MASK_BLEND_BASE_COLOR_ALPHA";
                }
                else
                {
                    yield return "VFX_MASK_BLEND_MASK_BLUE";
                }
                if (normalOpacityChannel == BlendSource.BaseColorMapAlpha)
                {
                    yield return "VFX_NORMAL_BLEND_BASE_COLOR_ALPHA";
                }
                else
                {
                    yield return "VFX_NORMAL_BLEND_MASK_BLUE";
                }

                if(enableDecalLayers)
                {
                    yield return "VFX_ENABLE_DECAL_LAYERS";
                }
                yield return "VFX_ENABLE_DECAL_LAYERS";
            }
        }


        protected override void WriteBlendMode(VFXShaderWriter writer)
        {
            // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
            for (int i = 0; i < 3; i++)
            {
                writer.WriteLineFormat("Blend {0} SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha", i);
            }
            writer.WriteLine("Blend 3 Zero OneMinusSrcColor");
        }
    }
}
