using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

namespace UnityEditor.VFX.HDRP
{
    abstract class VFXAbstractParticleHDRPLitOutput : VFXAbstractParticleHDRPOutput
    {
        public enum MaterialType
        {
            Standard,
            SpecularColor,
            Translucent,
            SimpleLit,
            SimpleLitTranslucent,
            SixWaySmokeLit,
        }
        private readonly string[] kMaterialTypeToName = new string[]
        {
            "StandardProperties",
            "SpecularColorProperties",
            "TranslucentProperties",
            "StandardProperties",
            "TranslucentProperties",
            "SixWaySmokeLitProperties"
        };

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

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles in this output are not affected by any lights in the scene and only receive ambient and light probe lighting.")]
        protected bool onlyAmbientLighting = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the diffusion profile to be used to simulate light passing through the particle. The diffusion profile needs to be also added to the HDRP asset or in the scene’s diffusion profile override.")]
        protected UnityEngine.Rendering.HighDefinition.DiffusionProfileSettings diffusionProfileAsset = null;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the thickness of the particle is multiplied with its alpha value.")]
        protected bool multiplyThicknessWithAlpha = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the normals of the particle are inverted when seen from behind, allowing quads with culling set to off to receive correct lighting information.")]
        protected bool doubleSided = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, specular lighting will be rendered regardless of opacity.")]
        protected bool preserveSpecularLighting = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Simple Lit features"), Tooltip("When enabled, particles will receive specular highlights.")]
        protected bool enableSpecular = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("\nSix-way Smoke Lit Settings"), Tooltip("Specifies how to remap the values in the lightmaps.")]
        protected LightmapRemapMode lightmapRemapMode = LightmapRemapMode.None;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Enables the modification of the light map ranges.")]

        protected bool lightmapRemapRanges = false;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the alpha of the particles can be remapped with the Alpha Remap curve.")]
        protected bool useAlphaRemap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip("Specifies what information is used to control the emissive color of the particle. It can come from the Alpha channel of the Negative Axes Lightmap or from an Emissive map.")]
        protected EmissiveMode emissiveMode = EmissiveMode.None;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, you can scale the values in the emissive channel before applying the Emissive Gradient.")]
        protected bool useEmissiveChannelScale = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the lightmaps are used to simulate color absorption whose strength can be tuned with the Absorption Strength parameter.")]
        protected bool useColorAbsorption = true;

        [FormerlySerializedAs("enableShadows")] [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the particle will receive shadows.")]
        protected bool receiveShadows = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles can be affected by light cookies.")]
        protected bool enableCookie = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles can be affected by environment light set in the global volume profile.")]
        protected bool enableEnvLight = true;

        protected override bool useEmissiveColor
        {
            get
            {
                if (materialType == MaterialType.SixWaySmokeLit) //In the SingleChannel mode, we use the gradient to control the color of the emissive
                    return emissiveMode == EmissiveMode.Map;
                return useEmissive;
            }
        }


        protected VFXAbstractParticleHDRPLitOutput(bool strip = false) : base(strip) { }

        public class HDRPLitInputProperties
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

        public class TranslucentProperties
        {
            [Range(0, 1), Tooltip("Sets the thickness of the translucent particle. This affects the influence of the diffusion profile.")]
            public float thickness = 1.0f;
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

        public class SixWaySmokeLitProperties
        {
            //Empty on purpose.
        }
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

        protected override RPInfo currentRP
        {
            get { return hdrpLitInfo; }
        }
        public override bool isLitShader { get => true; }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = Enumerable.Empty<VFXPropertyWithValue>();

                if (GetOrRefreshShaderGraphObject() == null)
                {
                    if (materialType == MaterialType.SixWaySmokeLit)
                        properties = properties.Concat(sixWayMapsProperties);
                    else
                        properties = properties.Concat(PropertiesFromType("HDRPLitInputProperties"));
                    properties = properties.Concat(PropertiesFromType(kMaterialTypeToName[(int)materialType]));

                }

                properties = properties.Concat(base.inputProperties);
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (GetOrRefreshShaderGraphObject() == null)
            {
                if(materialType != MaterialType.SixWaySmokeLit)
                    yield return slotExpressions.First(o => o.name == "smoothness");

                switch (materialType)
                {
                    case MaterialType.Standard:
                    case MaterialType.SimpleLit:
                        yield return slotExpressions.First(o => o.name == "metallic");
                        break;

                    case MaterialType.SpecularColor:
                        yield return slotExpressions.First(o => o.name == "specularColor");
                        break;

                    case MaterialType.Translucent:
                    case MaterialType.SimpleLitTranslucent:
                    {
                        yield return slotExpressions.First(o => o.name == "thickness");
                        uint diffusionProfileHash = (diffusionProfileAsset?.profile != null) ? diffusionProfileAsset.profile.hash : 0;
                        yield return new VFXNamedExpression(VFXValue.Constant(diffusionProfileHash), "diffusionProfileHash");
                        break;
                    }
                    case MaterialType.SixWaySmokeLit:
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
                            var absorptionRangeExp = (absorptionStrenghtExp + VFXValue.Constant(1.0f / Mathf.PI)) /
                                                     VFXValue.Constant(1 - 1.0f / Mathf.PI);
                            yield return new VFXNamedExpression(absorptionRangeExp, "absorptionRange");
                        }

                        break;

                    default:
                        break;
                }
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                if (GetOrRefreshShaderGraphObject() == null)
                    switch (materialType)
                    {
                        case MaterialType.Standard:
                            yield return "HDRP_MATERIAL_TYPE_STANDARD";
                            break;

                        case MaterialType.SpecularColor:
                            yield return "HDRP_MATERIAL_TYPE_SPECULAR";
                            break;

                        case MaterialType.Translucent:
                            yield return "HDRP_MATERIAL_TYPE_TRANSLUCENT";
                            if (multiplyThicknessWithAlpha)
                                yield return "HDRP_MULTIPLY_THICKNESS_WITH_ALPHA";
                            break;

                        case MaterialType.SimpleLit:
                            yield return "HDRP_MATERIAL_TYPE_SIMPLELIT";
                            yield return "NEEDS_DEPTH_FORWARD_ONLY";
                            if (receiveShadows)
                                yield return "HDRP_ENABLE_SHADOWS";
                            if (enableSpecular)
                                yield return "HDRP_ENABLE_SPECULAR";
                            if (enableCookie)
                                yield return "HDRP_ENABLE_COOKIE";
                            if (enableEnvLight)
                                yield return "HDRP_ENABLE_ENV_LIGHT";
                            break;

                        case MaterialType.SimpleLitTranslucent:
                            yield return "HDRP_MATERIAL_TYPE_SIMPLELIT_TRANSLUCENT";
                            if (receiveShadows)
                                yield return "HDRP_ENABLE_SHADOWS";
                            if (enableSpecular)
                                yield return "HDRP_ENABLE_SPECULAR";
                            if (enableCookie)
                                yield return "HDRP_ENABLE_COOKIE";
                            if (enableEnvLight)
                                yield return "HDRP_ENABLE_ENV_LIGHT";
                            if (multiplyThicknessWithAlpha)
                                yield return "HDRP_MULTIPLY_THICKNESS_WITH_ALPHA";
                            break;
                        case MaterialType.SixWaySmokeLit:
                            yield return "VFX_MATERIAL_TYPE_SIX_WAY_SMOKE";
                            yield return "NEEDS_DEPTH_FORWARD_ONLY";
                            if (receiveShadows)
                                yield return "HDRP_ENABLE_SHADOWS";
                            if (enableCookie)
                                yield return "HDRP_ENABLE_COOKIE";
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
                                yield return "VFX_SIX_WAY_ABSORPTION";
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
                            break;
                        default:
                            break;
                    }
                if (doubleSided)
                    yield return "USE_DOUBLE_SIDED";

                if (onlyAmbientLighting && !isBlendModeOpaque)
                    yield return "USE_ONLY_AMBIENT_LIGHTING";

                if (isBlendModeOpaque && (GetOrRefreshShaderGraphObject() != null ||
                                          materialType != MaterialType.SimpleLit &&
                                          materialType != MaterialType.SimpleLitTranslucent &&
                                          materialType != MaterialType.SixWaySmokeLit))
                    yield return "IS_OPAQUE_NOT_SIMPLE_LIT_PARTICLE";
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                if (materialType != MaterialType.Translucent && materialType != MaterialType.SimpleLitTranslucent)
                {
                    yield return nameof(diffusionProfileAsset);
                    yield return nameof(multiplyThicknessWithAlpha);
                }

                if (materialType != MaterialType.SimpleLit && materialType != MaterialType.SimpleLitTranslucent && materialType != MaterialType.SixWaySmokeLit)
                {
                    yield return nameof(receiveShadows);
                    if (materialType != MaterialType.SimpleLit && materialType != MaterialType.SimpleLitTranslucent)
                    {
                        yield return nameof(enableSpecular);
                        yield return nameof(enableCookie);
                        yield return nameof(enableEnvLight);
                    }
                }

                if (materialType == MaterialType.SixWaySmokeLit)
                {
                    yield return nameof(shaderGraph);
                    yield return nameof(preserveSpecularLighting);
                    yield return nameof(enableSpecular);
                    yield return nameof(doubleSided);
                    yield return nameof(enableEnvLight);
                    yield return nameof(useMaskMap);
                    yield return nameof(useNormalMap);
                    yield return nameof(useEmissiveMap);
                    yield return nameof(useEmissive);
                    yield return "normalBending";
                    if (emissiveMode != EmissiveMode.SingleChannel)
                        yield return nameof(useEmissiveChannelScale);
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
            }
        }
        public override void OnSettingModified(VFXSetting setting)
        {
            base.OnSettingModified(setting);
            if(setting.name == nameof(materialType) && (MaterialType)setting.value == MaterialType.SixWaySmokeLit)
            {
                useMaskMap = false;
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
                    default:
                        throw new InvalidEnumArgumentException();
                }
            }
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                foreach (var kvp in base.additionalReplacements)
                    yield return kvp;

                // HDRP Forward specific defines
                var forwardDefines = new VFXShaderWriter();
                forwardDefines.WriteLine("#define _ENABLE_FOG_ON_TRANSPARENT");
                forwardDefines.WriteLine("#define _DISABLE_DECALS");

                if (!isBlendModeOpaque)
                {
                    if (preserveSpecularLighting)
                        forwardDefines.WriteLine("#define SUPPORT_BLENDMODE_PRESERVE_SPECULAR_LIGHTING");
                    forwardDefines.WriteLineFormat("#define _EnableBlendModePreserveSpecularLighting {0}", preserveSpecularLighting ? 1 : 0 );
                }

                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXHDRPForwardDefines}", forwardDefines);
                var forwardPassName = new VFXShaderWriter();
                forwardPassName.Write(GetOrRefreshShaderGraphObject() == null &&
                                      (materialType == MaterialType.SimpleLit ||
                                       materialType == MaterialType.SimpleLitTranslucent ||
                                       materialType == MaterialType.SixWaySmokeLit)
                    ? "ForwardOnly"
                    : "Forward");
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXHDRPForwardPassName}", forwardPassName);
            }
        }
    }
}
