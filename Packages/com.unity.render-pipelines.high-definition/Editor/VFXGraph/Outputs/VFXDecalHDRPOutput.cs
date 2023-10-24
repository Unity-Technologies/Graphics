using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.VFX.HDRP
{
    [VFXInfo]
    class VFXDecalHDRPOutput : VFXAbstractParticleHDRPOutput
    {
        public override string name
        {
            get { return "Output Particle HDRP Lit Decal"; }
        }

        public override string codeGeneratorTemplate
        {
            get { return RenderPipeTemplate("VFXParticleHDRPDecal"); }
        }

        public override VFXTaskType taskType
        {
            get { return VFXTaskType.ParticleHexahedronOutput; }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            blendMode = BlendMode.Opaque;
            cullMode = CullMode.Back;
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
                if (usesFlipbook)
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
            }
        }


        public enum BlendSource
        {
            BaseColorMapAlpha,
            MaskMapBlue,
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Header("Opacity Channels"), SerializeField,
         Tooltip("Specifies the source this Material uses as opacity for its Normal Map.")]
        BlendSource normalOpacityChannel = BlendSource.BaseColorMapAlpha;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip("Specifies the source this Material uses as opacity for its Mask Map.")]
        BlendSource maskOpacityChannel = BlendSource.BaseColorMapAlpha;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Header("Surface options"), SerializeField,
         Tooltip("When enabled, modifies the base color of the surface it projects onto.")]
        private bool affectBaseColor = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip(
             "When enabled, modifies the metallic look of the surface it projects onto using the (R) channel of the Mask Map if one is provided.")]
        private bool affectMetal = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip(
             "When enabled, modifies the ambient occlusion (AO) of the surface it projects onto using the (G) channel of the Mask Map if one is provided.")]
        private bool affectAmbientOcclusion = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip(
             "When enabled, modifies the smoothness of the surface it projects onto using the (A) channel of the Mask Map if one is provided.")]
        private bool affectSmoothness = true;


        private bool supportDecals => HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportDecals &&
        HDRenderPipelineGlobalSettings.instance.GetDefaultFrameSettings(FrameSettingsRenderType.Camera).IsEnabled(FrameSettingsField.Decals);
        private bool enableDecalLayers =>
            supportDecals
            && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportDecalLayers
            && HDRenderPipelineGlobalSettings.instance.GetDefaultFrameSettings(FrameSettingsRenderType.Camera).IsEnabled(FrameSettingsField.DecalLayers);

        private bool metalAndAODecals =>
            supportDecals
            && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask;


        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip("Specify the layer mask for the decals. Unity renders decals on all meshes where at least one Rendering Layer value matches.")]
        private DecalLayerEnum decalLayer = DecalLayerEnum.DecalLayerDefault;

        private bool affectsAOAndHasMaskMap => affectAmbientOcclusion && useMaskMap;
        public override bool HasSorting() => (sort == SortActivationMode.On) || (sort == SortActivationMode.Auto);
        public override bool supportsUV { get { return GetOrRefreshShaderGraphObject() == null; } }
        protected override bool useNormalScale => false;

        public class FadeFactorProperty
        {
            [Range(0, 1), Tooltip("Controls the transparency of the decal.")]
            public float fadeFactor = 1.0f;
        }

        public class AngleFadeProperty
        {
            [Tooltip("Use the min-max slider to control the fade out range of the decal based on the angle between the Decal backward direction and the vertex normal of the receiving surface." +
                " Works only if Decal Layers is enabled both in the HDRP Asset and in the HDRP Global Settings."), MinMax(0.0f, 180.0f)]
            public Vector2 angleFade = new Vector2(0.0f, 180.0f);
        }

        public class NormalAlphaProperty
        {
            [Tooltip("Controls the blending factor of the normal map."), Range(0, 1)]
            public float normalAlpha = 1.0f;
        }

        protected IEnumerable<VFXPropertyWithValue> materialProperties
        {
            get
            {
                if (affectMetal)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float),
                        "metallic",
                        new TooltipAttribute(useMaskMap
                            ? "Controls the scale factor for the particle’s metallic."
                            : "Controls the metallic of the decal."),
                        new RangeAttribute(0, 1)), 0.0f);

                if (affectsAOAndHasMaskMap)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float),
                        "ambientOcclusion",
                        new TooltipAttribute("Controls the scale factor for the particle’s ambient occlusion."),
                        new RangeAttribute(0, 1)), 1.0f);

                if (affectSmoothness)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float),
                        "smoothness",
                        new TooltipAttribute(useMaskMap
                            ? "Controls the scale factor for the particle’s smoothness."
                            : "Controls the smoothness of the decal."),
                        new RangeAttribute(0, 1)), 0.5f);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = Enumerable.Empty<VFXPropertyWithValue>();

                properties = properties.Concat(PropertiesFromType(nameof(FadeFactorProperty)));
                properties = properties.Concat(PropertiesFromType(nameof(AngleFadeProperty)));

                foreach (var prop in base.inputProperties)
                {
                    properties = properties.Append(prop);
                    if(prop.property.name ==  "normalMap")
                        properties = properties.Concat(PropertiesFromType(nameof(NormalAlphaProperty)));
                }
                properties = properties.Concat(materialProperties);
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(
            IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (GetOrRefreshShaderGraphObject() == null)
            {
                yield return slotExpressions.First(o => o.name == nameof(FadeFactorProperty.fadeFactor));
                if (affectMetal)
                    yield return slotExpressions.First(o => o.name == "metallic");
                if (affectsAOAndHasMaskMap)
                    yield return slotExpressions.First(o => o.name == "ambientOcclusion");
                if (affectSmoothness)
                    yield return slotExpressions.First(o => o.name == "smoothness");


                var angleFadeExp = slotExpressions.First(o => o.name == nameof(AngleFadeProperty.angleFade));
                yield return new VFXNamedExpression(AngleFadeSimplification(angleFadeExp.exp), nameof(AngleFadeProperty.angleFade));
                if (useNormalMap)
                    yield return slotExpressions.First(o => o.name == nameof(NormalAlphaProperty.normalAlpha));
                yield return new VFXNamedExpression(VFXValue.Constant((uint)decalLayer), "decalLayerMask");
            }
        }

        VFXExpression AngleFadeSimplification(VFXExpression angleFadeExp)
        {
            // See DecalSystem.cs
            angleFadeExp = angleFadeExp / VFXValue.Constant(new Vector2(180.0f, 180.0f));
            var angleStart = new VFXExpressionExtractComponent(angleFadeExp, 0);
            var angleEnd = new VFXExpressionExtractComponent(angleFadeExp, 1);
            var range = new VFXExpressionMax(VFXValue.Constant(0.0001f), angleEnd - angleStart);
            var simplifiedAngleFade = new VFXExpressionCombine(
                VFXValue.Constant(0.222222222f) / range,
                (angleEnd - VFXValue.Constant(0.5f)) / range);
            return simplifiedAngleFade;
        }

        public override void OnSettingModified(VFXSetting setting)
        {
            base.OnSettingModified(setting);
            if (setting.name == "affectBaseColor")
            {
                if (!affectBaseColor)
                {
                    useBaseColorMap = BaseColorMapMode.Alpha;
                }
                else
                {
                    useBaseColorMap = BaseColorMapMode.ColorAndAlpha;
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
                yield return "cullMode";
                yield return "blendMode";
                yield return "doubleSided";
                yield return "shaderGraph";
                yield return "zTestMode";
                yield return "zWriteMode";
                yield return "castShadows";
                yield return "materialType";

                if (!enableDecalLayers)
                    yield return "decalLayer";
                if (!affectBaseColor)
                    yield return "useBaseColorMap";
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
                if (affectsAOAndHasMaskMap)
                    yield return "AFFECT_AMBIENT_OCCLUSION";
                if (affectSmoothness)
                    yield return "AFFECT_SMOOTHNESS";
                if (useEmissiveColor || useEmissiveMap)
                    yield return "NEEDS_FORWARD_EMISSIVE_PASS";
            }
        }

        protected VFXShaderWriter GetDecalMaskColor(int maskIndex)
        {
            var rs = new VFXShaderWriter();
            var maskString = "";
            switch (maskIndex)
            {
                case 0:
                    rs.Write(affectBaseColor ? "RBGA" : "0");
                    break;
                case 1:
                    rs.Write(useNormalMap ? "RGBA" : "0");
                    break;
                case 2:
                {
                    if (affectMetal)
                    {
                        maskString += "R";
                    }

                    if (affectsAOAndHasMaskMap)
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
                case 3:
                    if (affectMetal)
                    {
                        maskString += "R";
                    }

                    if (affectsAOAndHasMaskMap)
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
                    yield return new KeyValuePair<string, VFXShaderWriter>("${VFXDecalColorMask" + i + "}",
                        GetDecalMaskColor(i));
                }
            }
        }

        public override void Sanitize(int version)
        {
            base.Sanitize(version);
            VFXSlot oldNormalScaleSlot = null;
            VFXSlot newNormalAlphaSlot = null;
            foreach (var slot in inputSlots)
            {
                if (slot.name == "normalScale")
                {
                    oldNormalScaleSlot = slot;
                }

                if (slot.name == "normalAlpha")
                    newNormalAlphaSlot = slot;
            }

            if (oldNormalScaleSlot != null && newNormalAlphaSlot != null)
            {
                VFXSlot.CopyLinksAndValue(newNormalAlphaSlot, oldNormalScaleSlot, true);
                oldNormalScaleSlot.UnlinkAll(true, true);
            }
        }

        internal override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            base.GenerateErrors(manager);
            if (!supportDecals)
            {
                manager.RegisterError("DecalsDisabled", VFXErrorType.Warning,
                    $"Decals will not be rendered because the 'Decals' is disabled in your HDRP Asset. Enable 'Decals' in your HDRP Asset to make this output work.");
            }

            if (!enableDecalLayers)
            {
                manager.RegisterError("DecalLayersDisabled", VFXErrorType.Warning,
                    $"The Angle Fade parameter won't have any effect, because the 'Decal Layers' setting is disabled." +
                    $" Enable 'Decal Layers' in your HDRP Asset if you want to control the Angle Fade." +
                    $" There is a performance cost of enabling this option.");
            }

            if (!metalAndAODecals)
            {
                manager.RegisterError("DecalMetalAODisabled", VFXErrorType.Warning,
                    $"The Metallic and Ambient Occlusion parameters won't have any effect, because the 'Metal and AO properties' setting is disabled." +
                    $" Enable 'Metal and AO properties' in your HDRP Asset if you want to control the Metal and AO properties of decals. There is a performance cost of enabling this option.");
            }
        }

        protected override IEnumerable<string> untransferableSettings
        {
            get
            {
                foreach (var setting in base.untransferableSettings)
                {
                    yield return setting;
                }
                yield return "blendMode";
                yield return "cullMode";
            }
        }
    }
}
