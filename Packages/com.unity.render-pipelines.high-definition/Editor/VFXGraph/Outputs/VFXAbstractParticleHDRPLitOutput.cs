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

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("\nSmoke Lit Settings"),
         Tooltip("Specifies what information is used to control the emissive color of the particle. It can come from the Alpha channel of the Negative Axes Light Map or from an Emissive map.")]
        protected EmissiveMode smokeEmissiveMode = EmissiveMode.None;

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
                    return smokeEmissiveMode == EmissiveMode.Map;
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

        public class SixWaySmokeLitProperties
        {
            //Empty on purpose.
        }
        protected IEnumerable<VFXPropertyWithValue> sixWayMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), "positiveAxesLightMap", new TooltipAttribute("Specifies the light map for the positive axes, Right (R), Up (G), Back (B), and the opacity (A).")));
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), "negativeAxesLightMap", new TooltipAttribute("Specifies the light map for the Negative axes: Left (R), Bottom (G), Front (B), and the Emissive mask (A) for Single Channel emission mode.")));
                if (smokeEmissiveMode == EmissiveMode.SingleChannel)
                {
                    yield return new VFXPropertyWithValue(
                        new VFXProperty(typeof(Gradient), "emissiveGradient",
                            new TooltipAttribute("Remaps the values of the Emission channel.")),
                        VFXResources.defaultResources.gradientMapRamp);
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "emissiveMultiplier", new TooltipAttribute("Multiplies the values set in the Emissive Gradient."), new MinAttribute(0.0f)), 1.0f);
                }

                if (!isBlendModeOpaque)
                    yield return new VFXPropertyWithValue(
                        new VFXProperty(typeof(AnimationCurve), "alphaRemap",
                            new TooltipAttribute("Remaps the alpha value.")), AnimationCurve.Linear(0, 0, 1, 1));
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
                        yield return slotExpressions.First(o => o.name == "positiveAxesLightMap");
                        yield return slotExpressions.First(o => o.name == "negativeAxesLightMap");
                        if (smokeEmissiveMode == EmissiveMode.SingleChannel)
                        {
                            yield return slotExpressions.First(o => o.name == "emissiveGradient");
                            yield return slotExpressions.First(o => o.name == "emissiveMultiplier");
                        }

                        if (!isBlendModeOpaque)
                            yield return slotExpressions.First(o => o.name == "alphaRemap");
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
                            if (smokeEmissiveMode == EmissiveMode.SingleChannel)
                                yield return "VFX_SMOKE_USE_ONE_EMISSIVE_CHANNEL";
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
                    yield return "diffusionProfileAsset";
                    yield return "multiplyThicknessWithAlpha";
                }

                if (materialType != MaterialType.SimpleLit && materialType != MaterialType.SimpleLitTranslucent && materialType != MaterialType.SixWaySmokeLit)
                {
                    yield return "enableShadows";
                    if (materialType != MaterialType.SimpleLit && materialType != MaterialType.SimpleLitTranslucent)
                    {
                        yield return "enableSpecular";
                        yield return "enableTransmission";
                        yield return "enableCookie";
                        yield return "enableEnvLight";
                    }
                }

                if (materialType == MaterialType.SixWaySmokeLit)
                {
                    yield return "shaderGraph";
                    yield return "normalBending";
                    yield return "preserveSpecularLighting";
                    yield return "enableSpecular";
                    yield return "doubleSided";
                    yield return "enableEnvLight";
                    yield return "useMaskMap";
                    yield return "useNormalMap";
                    yield return "useEmissiveMap";
                    yield return "useEmissive";
                }
                else
                {
                    yield return "smokeEmissiveMode";
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

            if (setting.name == nameof(smokeEmissiveMode))
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
                        forwardDefines.WriteLine("#define _BLENDMODE_PRESERVE_SPECULAR_LIGHTING");
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
