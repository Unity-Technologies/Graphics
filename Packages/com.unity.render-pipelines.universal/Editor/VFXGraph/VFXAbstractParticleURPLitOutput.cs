#if HAS_VFX_GRAPH

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace UnityEditor.VFX.URP
{
    abstract class VFXAbstractParticleURPLitOutput : VFXShaderGraphParticleOutput
    {

        public enum MaterialType
        {
            Standard,
            SixWaySmokeLit,
        }

        public enum WorkflowMode
        {
            Metallic,
            Specular,
        }

        [Flags]
        public enum ColorMode
        {
            None = 0,
            BaseColor = 1 << 0,
            Emissive = 1 << 1,
            BaseColorAndEmissive = BaseColor | Emissive,
        }

        [Flags]
        public enum BaseColorMapMode
        {
            None = 0,
            Color = 1 << 0,
            Alpha = 1 << 1,
            ColorAndAlpha = Color | Alpha
        }

        public enum SmoothnessSource
        {
            None,
            MetallicAlpha,
            SpecularAlpha,
            AlbedoAlpha,
        }

        //TODO: Move these types to a utility in VFX package
        protected enum EmissiveMode
        {
            None,
            SingleChannel,
            Map,
        }

        protected enum LightmapRemapMode
        {
            None,
            ParametricContrast,
            CustomCurve,
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Lighting"), Tooltip("Specifies the surface type of this output. Surface types determine how the particle will react to light.")]
        protected MaterialType materialType = MaterialType.Standard;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, FormerlySerializedAs("materialType"), Tooltip("Select a workflow that fits your textures. Choose between Metallic or Specular.")]
        protected WorkflowMode workflowMode = WorkflowMode.Metallic;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specified the smoothness map source. It can be the alpha channel of the Metallic Map or the Base Color Map if they are used in the output.")]
        protected SmoothnessSource smoothnessSource = SmoothnessSource.None;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies what parts of the base color map is applied to the particles. Particles can receive color, alpha, color and alpha, or not receive any values from the base color map.")]
        protected BaseColorMapMode useBaseColorMap = BaseColorMapMode.ColorAndAlpha;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the output will accept an occlusion to control how the particle receives lighting.")]
        protected bool useOcclusionMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the output will accept a metallic map to multiply the metallic value.")]
        protected bool useMetallicMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the output will accept a metallic map to multiply the metallic value.")]
        protected bool useSpecularMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the output will accept a Normal Map to simulate additional surface details when illuminated.")]
        protected bool useNormalMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the output will accept an Emissive Map to control how particles glow.")]
        protected bool useEmissiveMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies how the color attribute is applied to the particles. It can be disregarded, used for the base color, used for the emissiveness, or used for both the base color and the emissiveness.")]
        protected ColorMode colorMode = ColorMode.BaseColor;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("\nSix-way Smoke Lit Settings"), Tooltip("Specifies how to remap the values in the lightmaps.")]
        protected LightmapRemapMode lightmapRemapMode = LightmapRemapMode.None;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Enables the modification of the light map ranges.")]
        protected bool lightmapRemapRanges = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the alpha of the particles can be remapped with the Alpha Remap curve.")]
        protected bool useAlphaRemap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, an emissive color field becomes available in the output to make particles glow.")]
        protected bool useEmissive = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip("Specifies what information is used to control the emissive color of the particle. It can come from the Alpha channel of the Negative Axes Lightmap or from an Emissive map.")]
        protected EmissiveMode emissiveMode = EmissiveMode.None;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, you can scale the values in the emissive channel before applying the Emissive Gradient.")]
        protected bool useEmissiveChannelScale = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the lightmaps are used to simulate color absorption whose strength can be tuned with the Absorption Strength parameter.")]
        protected bool useColorAbsorption = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the normals of the particle are inverted when seen from behind, allowing quads with culling set to off to receive correct lighting information.")]
        protected bool doubleSided = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the particle will receive shadows.")]
        protected bool receiveShadows = true;

        protected bool useEmissiveColor
        {
            get
            {
                if (materialType == MaterialType.SixWaySmokeLit) //In the SingleChannel mode, we use the gradient to control the color of the emissive
                    return emissiveMode == EmissiveMode.Map;
                return useEmissive;
            }
        }

        protected VFXAbstractParticleURPLitOutput(bool strip = false) : base(strip) {}

        protected virtual bool allowTextures => GetOrRefreshShaderGraphObject() == null;

        protected virtual bool useSmoothness => true;
        protected virtual bool useMetallic => workflowMode == WorkflowMode.Metallic;
        protected virtual bool useSpecular => workflowMode == WorkflowMode.Specular;
        protected virtual bool useNormalScale => true;


        public class URPLitInputProperties
        {
            [Range(0, 1), Tooltip("Controls the scale factor for the particle’s smoothness.")]
            public float smoothness = 0.5f;
        }

        public class StandardProperties
        {
            [Range(0, 1), Tooltip("Controls the scale factor for the particle’s metallicity.")]
            public float metallic = 0.0f;
        }

        public class SpecularColorProperties
        {
            [Tooltip("Sets the specular color of the particle.")]
            public Color specularColor = Color.gray;
        }

        public override sealed bool CanBeCompiled()
        {
            return (VFXLibrary.currentSRPBinder is VFXURPBinder) && base.CanBeCompiled();
        }

        private IEnumerable<SmoothnessSource> validSmoothnessSources
        {
            get
            {
                yield return SmoothnessSource.None;
                if (useBaseColorMap != BaseColorMapMode.None)
                    yield return SmoothnessSource.AlbedoAlpha;
                if (workflowMode == WorkflowMode.Metallic && useMetallicMap)
                    yield return SmoothnessSource.MetallicAlpha;
                if (workflowMode == WorkflowMode.Specular && useSpecularMap)
                    yield return SmoothnessSource.SpecularAlpha;
            }
        }

        public override IEnumerable<int> GetFilteredOutEnumerators(string name)
        {
            if (name == nameof(smoothnessSource))
            {
                var all = Enum.GetValues(typeof(SmoothnessSource)).Cast<int>();
                var valid = validSmoothnessSources.Cast<int>();
                return all.Except(valid);
            }

            return base.GetFilteredOutEnumerators(name);
        }

        protected internal override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);

            if (cause == InvalidationCause.kSettingChanged
                && !validSmoothnessSources.Contains(smoothnessSource))
            {
                var fallbackSmoothness = SmoothnessSource.None;
                if (smoothnessSource == SmoothnessSource.MetallicAlpha && validSmoothnessSources.Contains(SmoothnessSource.SpecularAlpha))
                    fallbackSmoothness = SmoothnessSource.SpecularAlpha;
                if (smoothnessSource == SmoothnessSource.SpecularAlpha && validSmoothnessSources.Contains(SmoothnessSource.MetallicAlpha))
                    fallbackSmoothness = SmoothnessSource.MetallicAlpha;

                SetSettingValue(nameof(smoothnessSource), fallbackSmoothness);
            }
        }

        private static readonly string kBaseColorMap = "baseColorMap";
        protected IEnumerable<VFXPropertyWithValue> baseColorMapProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), kBaseColorMap, new TooltipAttribute("Specifies the base color (RGB) and opacity (A) of the particle.")), (usesFlipbook ? null : VFXResources.defaultResources.particleTexture));
            }
        }

        private static readonly string kOcclusionMap = "occlusionMap";
        protected IEnumerable<VFXPropertyWithValue> occlusionMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), kOcclusionMap, new TooltipAttribute("Specifies the Occlusion Map for the particle - Ambient occlusion (G)")));
            }
        }

        private static readonly string kSpecularMap = "specularMap";
        protected IEnumerable<VFXPropertyWithValue> specularMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), kSpecularMap, new TooltipAttribute("Specifies the Specular Map for the particle - Color (RGB) - (Optional A) Smoothness")));
            }
        }

        private static readonly string kMetallicMap = "metallicMap";
        protected IEnumerable<VFXPropertyWithValue> metallicMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), kMetallicMap, new TooltipAttribute("Specifies the Metallic Map for the particle - Metallic (R) - (Optional A) Smoothness")));
            }
        }

        private static readonly string kNormalMap = "normalMap";
        private static readonly string kNormalScale = "normalScale";
        protected IEnumerable<VFXPropertyWithValue> normalMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), kNormalMap, new TooltipAttribute("Specifies the Normal map to obtain normals in tangent space for the particle.")));
                if(useNormalScale)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), kNormalScale, new TooltipAttribute("Sets the scale of the normals. Larger values increase the impact of the normals.")), 1.0f);
            }
        }

        private static readonly string kEmissiveMap = "emissiveMap";
        private static readonly string kEmissiveScale = "emissiveScale";
        protected IEnumerable<VFXPropertyWithValue> emissiveMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), kEmissiveMap, new TooltipAttribute("Specifies the Emissive map (RGB) used to make particles glow.")));
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), kEmissiveScale, new TooltipAttribute("Sets the scale of the emission obtained from the emissive map.")), 1.0f);
            }
        }

        public class BaseColorProperties
        {
            [Tooltip("Sets the base color of the particle.")]
            public Color baseColor = Color.white;
        }

        public class EmissiveColorProperties
        {
            [Tooltip("Sets the emissive color to make particles glow.")]
            public Color emissiveColor = Color.black;
        }

        public class SixWayParametricContrastProperties
        {
            [Range(-5, 5), Tooltip("Sets the contrast strength applied the lightmaps.")]
            public float contrastIntensity = 0.0f;
            [Range(0, 1), Tooltip("Specifies on which value of the lightmap the contrast transition happens. If Contrast Intensity is zero, this parameter has not effect.")]
            public float contrastPivot = 0.5f;
        }

        public class SixWayRemapRangeProperties
        {
            [Tooltip("Sets the source range of the lightmaps used for remapping.")]
            public Vector2 remapFrom = new Vector2(0, 1);
            [Tooltip("Sets the output range of the lightmaps.")]
            public Vector2 remapTo = new Vector2(0, 1);
        }

        protected override bool needsExposureWeight { get { return GetOrRefreshShaderGraphObject() == null && ((colorMode & ColorMode.Emissive) != 0 || useEmissive || useEmissiveMap); } }

        protected override bool bypassExposure { get { return false; } }

        protected override VFXOldShaderGraphHelpers.RPInfo currentRP => VFXOldShaderGraphHelpers.urpLitInfo;

        public override bool isLitShader => true;

        protected IEnumerable<VFXPropertyWithValue> sixWayMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), "positiveAxesLightmap", new TooltipAttribute("Specifies the lightmap for the positive axes, Right (R), Up (G), Back (B), and the opacity (A).")));
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), "negativeAxesLightmap", new TooltipAttribute("Specifies the lightmap for the Negative axes: Left (R), Bottom (G), Front (B), and the Emissive mask (A) for Single Channel emission mode.")));

                if (lightmapRemapRanges)
                {
                    foreach (var prop in PropertiesFromType("SixWayRemapRangeProperties"))
                        yield return prop;
                }
                if (lightmapRemapMode == LightmapRemapMode.ParametricContrast)
                {
                    foreach (var prop in PropertiesFromType("SixWayParametricContrastProperties"))
                        yield return prop;
                }
                if (lightmapRemapMode == LightmapRemapMode.CustomCurve)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), "lightRemapCurve"),
                        AnimationCurve.Linear(0, 0, 1, 1));

                if (!isBlendModeOpaque && useAlphaRemap)
                    yield return new VFXPropertyWithValue(
                        new VFXProperty(typeof(AnimationCurve), "alphaRemap",
                            new TooltipAttribute("Remaps the alpha value.")), AnimationCurve.Linear(0, 0, 1, 1));

                if (emissiveMode == EmissiveMode.SingleChannel)
                {
                    if(useEmissiveChannelScale)
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "emissiveChannelScale", new TooltipAttribute("Multiplies the value contained in the Emission channel, before applying the gradient."), new RangeAttribute(0.0f, 1.0f)), 1.0f);

                    yield return new VFXPropertyWithValue(
                        new VFXProperty(typeof(Gradient), "emissiveGradient",
                            new TooltipAttribute("Remaps the values of the Emission channel.")),
                        VFXResources.defaultResources.gradientMapRamp);
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "emissiveMultiplier", new TooltipAttribute("Multiplies the values set in the Emissive Gradient."), new MinAttribute(0.0f)), 1.0f);

                }
                if(useColorAbsorption)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "absorptionStrength", new TooltipAttribute("Sets the strength of the color absorption."), new RangeAttribute(0.0f, 1.0f)), 0.5f);

            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;

                if (GetOrRefreshShaderGraphObject() == null)
                {
                    if (materialType == MaterialType.Standard)
                    {
                        if(useSmoothness)
                            properties = properties.Concat(PropertiesFromType(nameof(URPLitInputProperties)));
                        if(useMetallic)
                            properties = properties.Concat(PropertiesFromType(nameof(StandardProperties)));
                        if(useSpecular)
                            properties = properties.Concat(PropertiesFromType(nameof(SpecularColorProperties)));
                    }
                    else if(materialType == MaterialType.SixWaySmokeLit)
                        properties = properties.Concat(sixWayMapsProperties);

                    if (allowTextures)
                    {
                        if (useBaseColorMap != BaseColorMapMode.None)
                            properties = properties.Concat(baseColorMapProperties);
                    }

                    if ((colorMode & ColorMode.BaseColor) == 0) // particle color is not used as base color so add a slot
                        properties = properties.Concat(PropertiesFromType(nameof(BaseColorProperties)));

                    if (allowTextures)
                    {
                        if (useOcclusionMap)
                            properties = properties.Concat(occlusionMapsProperties);
                        if (useMetallicMap && workflowMode == WorkflowMode.Metallic)
                            properties = properties.Concat(metallicMapsProperties);
                        if (useSpecularMap && workflowMode == WorkflowMode.Specular)
                            properties = properties.Concat(specularMapsProperties);
                        if (useNormalMap)
                            properties = properties.Concat(normalMapsProperties);
                        if (useEmissiveMap)
                            properties = properties.Concat(emissiveMapsProperties);
                    }

                    if (((colorMode & ColorMode.Emissive) == 0) && useEmissiveColor)
                        properties = properties.Concat(PropertiesFromType(nameof(EmissiveColorProperties)));
                }

                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (GetOrRefreshShaderGraphObject() == null)
            {
                if (materialType == MaterialType.SixWaySmokeLit)
                {
                    yield return slotExpressions.First(o => o.name == "positiveAxesLightmap");
                    yield return slotExpressions.First(o => o.name == "negativeAxesLightmap");

                    if (emissiveMode == EmissiveMode.SingleChannel)
                    {
                        yield return slotExpressions.First(o => o.name == "emissiveGradient");
                        yield return slotExpressions.First(o => o.name == "emissiveMultiplier");
                        if(useEmissiveChannelScale)
                            yield return slotExpressions.First(o => o.name == "emissiveChannelScale");
                    }
                    if (!isBlendModeOpaque && useAlphaRemap)
                        yield return slotExpressions.First(o => o.name == "alphaRemap");

                    if (lightmapRemapRanges)
                    {
                        yield return slotExpressions.First(o => o.name == "remapFrom");
                        yield return slotExpressions.First(o => o.name == "remapTo");
                    }

                    if (lightmapRemapMode == LightmapRemapMode.ParametricContrast)
                    {
                        var lightmapBrightnessExp = slotExpressions.First(o => o.name == "contrastPivot").exp;
                        var rawLightMapContrastExp = slotExpressions.First(o => o.name == "contrastIntensity").exp;
                        var lightmapContrastExp = VFXOperatorUtility.Exp(rawLightMapContrastExp, VFXOperatorUtility.Base.Base2);
                        var lightmapControlsExp = new VFXExpressionCombine(lightmapBrightnessExp, lightmapContrastExp);
                        yield return new VFXNamedExpression(lightmapControlsExp, "lightmapRemapControls");
                    }

                    if(lightmapRemapMode == LightmapRemapMode.CustomCurve)
                        yield return slotExpressions.First(o => o.name == "lightRemapCurve");
                    if (useColorAbsorption)
                    {
                        var absorptionStrenghtExp = slotExpressions.First(o => o.name == "absorptionStrength").exp;
                        var absorptionRangeExp = (VFXValue.Constant(1 - 1.0f / Mathf.PI) * absorptionStrenghtExp + VFXValue.Constant(1.0f / Mathf.PI));

                        yield return new VFXNamedExpression(absorptionRangeExp, "absorptionRange");
                    }
                    if (allowTextures)
                    {
                        if (useBaseColorMap != BaseColorMapMode.None)
                            yield return slotExpressions.First(o => o.name == kBaseColorMap);
                        if (useEmissiveMap)
                        {
                            yield return slotExpressions.First(o => o.name == kEmissiveMap);
                            yield return slotExpressions.First(o => o.name == kEmissiveScale);
                        }
                    }
                }
                else
                {
                    if(useSmoothness)
                        yield return slotExpressions.First(o => o.name == nameof(URPLitInputProperties.smoothness));

                    switch (workflowMode)
                    {
                        case WorkflowMode.Metallic:
                            if(useMetallic)
                                yield return slotExpressions.First(o => o.name == nameof(StandardProperties.metallic));
                            break;
                        case WorkflowMode.Specular:
                            if(useSpecular)
                                yield return slotExpressions.First(o => o.name == nameof(SpecularColorProperties.specularColor));
                            break;
                        default:
                            break;
                    }

                    if (allowTextures)
                    {
                        if (useBaseColorMap != BaseColorMapMode.None)
                            yield return slotExpressions.First(o => o.name == kBaseColorMap);
                        if (useOcclusionMap)
                            yield return slotExpressions.First(o => o.name == kOcclusionMap);
                        if (useMetallicMap && workflowMode == WorkflowMode.Metallic)
                            yield return slotExpressions.First(o => o.name == kMetallicMap);
                        if (useSpecularMap && workflowMode == WorkflowMode.Specular)
                            yield return slotExpressions.First(o => o.name == kSpecularMap);
                        if (useNormalMap)
                        {
                            yield return slotExpressions.First(o => o.name == kNormalMap);
                            if(useNormalScale)
                            	yield return slotExpressions.First(o => o.name == kNormalScale);
                        }

                        if (useEmissiveMap)
                        {
                            yield return slotExpressions.First(o => o.name == kEmissiveMap);
                            yield return slotExpressions.First(o => o.name == kEmissiveScale);
                        }
                    }
                }

                if ((colorMode & ColorMode.BaseColor) == 0)
                    yield return slotExpressions.First(o => o.name == nameof(BaseColorProperties.baseColor));

                if (((colorMode & ColorMode.Emissive) == 0) && useEmissiveColor)
                    yield return slotExpressions.First(o =>
                        o.name == nameof(EmissiveColorProperties.emissiveColor));
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                yield return "URP_LIT";

                if (GetOrRefreshShaderGraphObject() == null)
                {
                    if (!receiveShadows)
                        yield return "_RECEIVE_SHADOWS_OFF";

                    if (materialType == MaterialType.SixWaySmokeLit)
                    {
                        yield return "VFX_MATERIAL_TYPE_SIX_WAY_SMOKE";
                        if (emissiveMode == EmissiveMode.SingleChannel)
                        {
                            yield return "VFX_SIX_WAY_USE_ONE_EMISSIVE_CHANNEL";
                            if (useEmissiveChannelScale)
                                yield return "VFX_SIX_WAY_EMISSIVE_CHANNEL_SCALE";
                        }
                        if (!isBlendModeOpaque && useAlphaRemap)
                            yield return "VFX_SIX_WAY_USE_ALPHA_REMAP";

                        if(lightmapRemapMode != LightmapRemapMode.None || lightmapRemapRanges)
                            yield return "VFX_SIX_WAY_REMAP";

                        if (lightmapRemapRanges)
                            yield return "VFX_SIX_WAY_REMAP_RANGES";
                        if (useColorAbsorption)
                        {
                            yield return "VFX_SIX_WAY_COLOR_ABSORPTION";
                        }
                        switch (lightmapRemapMode)
                        {
                            case LightmapRemapMode.ParametricContrast:
                                yield return "VFX_SIX_WAY_REMAP_NONLIN";
                                break;
                            case LightmapRemapMode.CustomCurve:
                                yield return "VFX_SIX_WAY_REMAP_CURVE";
                                break;
                        }

                    }
                    else
                    {
                        switch (workflowMode)
                        {
                            case WorkflowMode.Metallic:
                                yield return "URP_WORKFLOW_MODE_METALLIC";
                                break;

                            case WorkflowMode.Specular:
                                yield return "URP_WORKFLOW_MODE_SPECULAR";
                                break;
                        }
                    }
                }


                if (allowTextures)
                {
                    if (useBaseColorMap != BaseColorMapMode.None)
                        yield return "URP_USE_BASE_COLOR_MAP";
                    if ((useBaseColorMap & BaseColorMapMode.Color) != 0)
                        yield return "URP_USE_BASE_COLOR_MAP_COLOR";
                    if ((useBaseColorMap & BaseColorMapMode.Alpha) != 0)
                        yield return "URP_USE_BASE_COLOR_MAP_ALPHA";
                    if (useOcclusionMap)
                        yield return "URP_USE_OCCLUSION_MAP";
                    if (useMetallicMap && workflowMode == WorkflowMode.Metallic)
                        yield return "URP_USE_METALLIC_MAP";
                    if (useSpecularMap && workflowMode == WorkflowMode.Specular)
                        yield return "URP_USE_SPECULAR_MAP";
                    if (useNormalMap)
                        yield return "USE_NORMAL_MAP";
                    if (useEmissiveMap)
                        yield return "URP_USE_EMISSIVE_MAP";
                }

                if (GetOrRefreshShaderGraphObject() == null)
                {
                    if ((colorMode & ColorMode.BaseColor) != 0)
                        yield return "URP_USE_BASE_COLOR";
                    else
                        yield return "URP_USE_ADDITIONAL_BASE_COLOR";

                    if ((colorMode & ColorMode.Emissive) != 0)
                        yield return "URP_USE_EMISSIVE_COLOR";
                    else if (useEmissiveColor)
                        yield return "URP_USE_ADDITIONAL_EMISSIVE_COLOR";

                    switch (smoothnessSource)
                    {
                        case SmoothnessSource.None: yield return "URP_USE_SMOOTHNESS_IN_NONE"; break;
                        case SmoothnessSource.MetallicAlpha: yield return "URP_USE_SMOOTHNESS_IN_METALLIC"; break;
                        case SmoothnessSource.SpecularAlpha: yield return "URP_USE_SMOOTHNESS_IN_SPECULAR"; break;
                        case SmoothnessSource.AlbedoAlpha: yield return "URP_USE_SMOOTHNESS_IN_ALBEDO"; break;
                    }

                }

                if (doubleSided)
                    yield return "USE_DOUBLE_SIDED";
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return nameof(colorMapping);

                if (!allowTextures)
                {
                    yield return nameof(useBaseColorMap);
                    yield return nameof(useOcclusionMap);
                    yield return nameof(useMetallicMap);
                    yield return nameof(useSpecularMap);
                    yield return nameof(useNormalMap);
                    yield return nameof(useEmissiveMap);
                }

                if (workflowMode != WorkflowMode.Metallic)
                    yield return nameof(useMetallicMap);

                if (workflowMode != WorkflowMode.Specular)
                    yield return nameof(useSpecularMap);

                if (materialType == MaterialType.SixWaySmokeLit)
                {
                    yield return nameof(doubleSided);
                    yield return nameof(useMetallicMap);
                    yield return nameof(useOcclusionMap);
                    yield return nameof(useSpecularMap);
                    yield return nameof(useNormalMap);
                    yield return nameof(useEmissiveMap);
                    yield return nameof(useEmissive);
                    yield return nameof(smoothnessSource);
                    yield return nameof(workflowMode);
                    if (emissiveMode != EmissiveMode.SingleChannel)
                        yield return nameof(useEmissiveChannelScale);
                    yield return "normalBending";
                }
                else
                {
                    yield return nameof(useColorAbsorption);
                    yield return nameof(emissiveMode);
                    yield return nameof(useEmissiveChannelScale);
                    yield return nameof(lightmapRemapMode);
                    yield return nameof(useAlphaRemap);
                    yield return nameof(lightmapRemapRanges);
                }

                if (GetOrRefreshShaderGraphObject() != null)
                {
                    yield return nameof(workflowMode);
                    yield return nameof(useEmissive);
                    yield return nameof(colorMode);
                    yield return nameof(smoothnessSource);
                    yield return nameof(useColorAbsorption);
                    yield return nameof(emissiveMode);
                    yield return nameof(useEmissiveChannelScale);
                    yield return nameof(lightmapRemapMode);
                    yield return nameof(useAlphaRemap);
                    yield return nameof(lightmapRemapRanges);
                    yield return nameof(receiveShadows);
                }
                else if ((colorMode & ColorMode.Emissive) != 0)
                    yield return nameof(useEmissive);

                yield return nameof(excludeFromTUAndAA);
            }
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

                foreach (var attribute in flipbookAttributes)
                    yield return attribute;
            }
        }

        public override void OnEnable()
        {
            colorMapping = ColorMappingMode.Default;
            base.OnEnable();
        }

        public override void OnSettingModified(VFXSetting setting)
        {
            base.OnSettingModified(setting);
            if (setting.name == nameof(materialType) && (MaterialType)setting.value == MaterialType.SixWaySmokeLit)
            {
                useOcclusionMap = false;
                useMetallicMap = false;
                useSpecularMap = false;
                useNormalMap = false;
                useBaseColorMap = BaseColorMapMode.None;
                shaderGraph = null;
                doubleSided = true;
            }
            if (setting.name == nameof(emissiveMode))
            {
                switch ((EmissiveMode)setting.value)
                {
                    case EmissiveMode.None:
                        useEmissive = false;
                        useEmissiveMap = false;
                        break;
                    case EmissiveMode.SingleChannel:
                        useEmissive = true;
                        useEmissiveMap = false;
                        break;
                    case EmissiveMode.Map:
                        useEmissive = true;
                        useEmissiveMap = true;
                        break;
                }
            }
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                foreach (var kvp in base.additionalReplacements)
                    yield return kvp;

                // URP Forward specific defines
                var forwardDefines = new VFXShaderWriter();
                if (workflowMode == WorkflowMode.Specular)
                    forwardDefines.Write("#define _SPECULAR_SETUP");
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXURPForwardDefines}", forwardDefines);

                // URP GBuffer specific defines
                var gbufferDefines = new VFXShaderWriter();
                if (workflowMode == WorkflowMode.Specular)
                    gbufferDefines.Write("#define _SPECULAR_SETUP");
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXURPGBufferDefines}", gbufferDefines);

                var forwardPassName = new VFXShaderWriter();
                forwardPassName.Write(materialType == MaterialType.SixWaySmokeLit ? "UniversalForwardOnly" : "UniversalForward");
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXURPForwardPassName}", forwardPassName);
            }
        }
    }
}
#endif
