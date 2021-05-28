using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractParticleHDRPLitOutput : VFXShaderGraphParticleOutput
    {
        public enum MaterialType
        {
            Standard,
            SpecularColor,
            Translucent,
            SimpleLit,
            SimpleLitTranslucent,
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

        private readonly string[] kMaterialTypeToName = new string[]
        {
            "StandardProperties",
            "SpecularColorProperties",
            "TranslucentProperties",
            "StandardProperties",
            "TranslucentProperties",
        };

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Lighting"), Tooltip("Specifies the surface type of this output. Surface types determine how the particle will react to light.")]
        protected MaterialType materialType = MaterialType.Standard;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles in this output are not affected by any lights in the scene and only receive ambient and light probe lighting.")]
        protected bool onlyAmbientLighting = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the diffusion profile to be used to simulate light passing through the particle. The diffusion profile needs to be also added to the HDRP asset or in the scene’s diffusion profile override.")]
        protected UnityEngine.Rendering.HighDefinition.DiffusionProfileSettings diffusionProfileAsset = null;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the thickness of the particle is multiplied with its alpha value.")]
        protected bool multiplyThicknessWithAlpha = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies what parts of the base color map is applied to the particles. Particles can receive color, alpha, color and alpha, or not receive any values from the base color map.")]
        protected BaseColorMapMode useBaseColorMap = BaseColorMapMode.ColorAndAlpha;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the output will accept a Mask Map to control how the particle receives lighting.")]
        protected bool useMaskMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the output will accept a Normal Map to simulate additional surface details when illuminated.")]
        protected bool useNormalMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the output will accept an Emissive Map to control how particles glow.")]
        protected bool useEmissiveMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies how the color attribute is applied to the particles. It can be disregarded, used for the base color, used for the emissiveness, or used for both the base color and the emissiveness.")]
        protected ColorMode colorMode = ColorMode.BaseColor;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, an emissive color field becomes available in the output to make particles glow.")]
        protected bool useEmissive = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the normals of the particle are inverted when seen from behind, allowing quads with culling set to off to receive correct lighting information.")]
        protected bool doubleSided = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, specular lighting will be rendered regardless of opacity.")]
        protected bool preserveSpecularLighting = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Simple Lit features"), Tooltip("When enabled, the particle will receive shadows.")]
        protected bool enableShadows = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles will receive specular highlights.")]
        protected bool enableSpecular = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles can be affected by light cookies.")]
        protected bool enableCookie = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles can be affected by environment light set in the global volume profile.")]
        protected bool enableEnvLight = true;

        protected VFXAbstractParticleHDRPLitOutput(bool strip = false) : base(strip) { }

        protected virtual bool allowTextures { get { return GetOrRefreshShaderGraphObject() == null; }}

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

        public class BaseColorMapProperties
        {
            [Tooltip("Specifies the base color (RGB) and opacity (A) of the particle.")]
            public Texture2D baseColorMap = VFXResources.defaultResources.particleTexture;
        }

        public class MaskMapProperties
        {
            [Tooltip("Specifies the Mask Map for the particle - Metallic (R), Ambient occlusion (G), and Smoothness (A).")]
            public Texture2D maskMap = VFXResources.defaultResources.noiseTexture;
        }

        public class NormalMapProperties
        {
            [Tooltip("Specifies the Normal map to obtain normals in tangent space for the particle.")]
            public Texture2D normalMap = null; // TODO Add normal map to default resources
            [Range(0, 2), Tooltip("Sets the scale of the normals. Larger values increase the impact of the normals.")]
            public float normalScale = 1.0f;
        }

        public class EmissiveMapProperties
        {
            [Tooltip("Specifies the Emissive map (RGB) used to make particles glow.")]
            public Texture2D emissiveMap = null;
            [Tooltip("Sets the scale of the emission obtained from the emissive map.")]
            public float emissiveScale = 1.0f;
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

        protected override bool needsExposureWeight { get { return GetOrRefreshShaderGraphObject() == null && ((colorMode & ColorMode.Emissive) != 0 || useEmissive || useEmissiveMap); } }

        protected override bool bypassExposure { get { return false; } }

        protected override RPInfo currentRP
        {
            get { return hdrpLitInfo; }
        }
        public override bool isLitShader { get => true; }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;

                if (GetOrRefreshShaderGraphObject() == null)
                {
                    properties = properties.Concat(PropertiesFromType("HDRPLitInputProperties"));
                    properties = properties.Concat(PropertiesFromType(kMaterialTypeToName[(int)materialType]));

                    if (allowTextures)
                    {
                        if (useBaseColorMap != BaseColorMapMode.None)
                            properties = properties.Concat(PropertiesFromType("BaseColorMapProperties"));
                    }

                    if ((colorMode & ColorMode.BaseColor) == 0) // particle color is not used as base color so add a slot
                        properties = properties.Concat(PropertiesFromType("BaseColorProperties"));

                    if (allowTextures)
                    {
                        if (useMaskMap)
                            properties = properties.Concat(PropertiesFromType("MaskMapProperties"));
                        if (useNormalMap)
                            properties = properties.Concat(PropertiesFromType("NormalMapProperties"));
                        if (useEmissiveMap)
                            properties = properties.Concat(PropertiesFromType("EmissiveMapProperties"));
                    }

                    if (((colorMode & ColorMode.Emissive) == 0) && useEmissive)
                        properties = properties.Concat(PropertiesFromType("EmissiveColorProperties"));
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

                    default: break;
                }

                if (allowTextures)
                {
                    if (useBaseColorMap != BaseColorMapMode.None)
                        yield return slotExpressions.First(o => o.name == "baseColorMap");
                    if (useMaskMap)
                        yield return slotExpressions.First(o => o.name == "maskMap");
                    if (useNormalMap)
                    {
                        yield return slotExpressions.First(o => o.name == "normalMap");
                        yield return slotExpressions.First(o => o.name == "normalScale");
                    }
                    if (useEmissiveMap)
                    {
                        yield return slotExpressions.First(o => o.name == "emissiveMap");
                        yield return slotExpressions.First(o => o.name == "emissiveScale");
                    }
                }

                if ((colorMode & ColorMode.BaseColor) == 0)
                    yield return slotExpressions.First(o => o.name == "baseColor");

                if (((colorMode & ColorMode.Emissive) == 0) && useEmissive)
                    yield return slotExpressions.First(o => o.name == "emissiveColor");
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                yield return "HDRP_LIT";

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
                            if (enableShadows)
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
                            if (enableShadows)
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

                        default: break;
                    }

                if (allowTextures)
                {
                    if (useBaseColorMap != BaseColorMapMode.None)
                        yield return "HDRP_USE_BASE_COLOR_MAP";
                    if ((useBaseColorMap & BaseColorMapMode.Color) != 0)
                        yield return "HDRP_USE_BASE_COLOR_MAP_COLOR";
                    if ((useBaseColorMap & BaseColorMapMode.Alpha) != 0)
                        yield return "HDRP_USE_BASE_COLOR_MAP_ALPHA";
                    if (useMaskMap)
                        yield return "HDRP_USE_MASK_MAP";
                    if (useNormalMap)
                        yield return "USE_NORMAL_MAP";
                    if (useEmissiveMap)
                        yield return "HDRP_USE_EMISSIVE_MAP";
                }

                if (GetOrRefreshShaderGraphObject() == null)
                {
                    if ((colorMode & ColorMode.BaseColor) != 0)
                        yield return "HDRP_USE_BASE_COLOR";
                    else
                        yield return "HDRP_USE_ADDITIONAL_BASE_COLOR";

                    if ((colorMode & ColorMode.Emissive) != 0)
                        yield return "HDRP_USE_EMISSIVE_COLOR";
                    else if (useEmissive)
                        yield return "HDRP_USE_ADDITIONAL_EMISSIVE_COLOR";
                }

                if (doubleSided)
                    yield return "USE_DOUBLE_SIDED";

                if (onlyAmbientLighting && !isBlendModeOpaque)
                    yield return "USE_ONLY_AMBIENT_LIGHTING";

                if (isBlendModeOpaque && (GetOrRefreshShaderGraphObject() != null || (materialType != MaterialType.SimpleLit && materialType != MaterialType.SimpleLitTranslucent)))
                    yield return "IS_OPAQUE_NOT_SIMPLE_LIT_PARTICLE";
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return "colorMapping";

                if (materialType != MaterialType.Translucent && materialType != MaterialType.SimpleLitTranslucent)
                {
                    yield return "diffusionProfileAsset";
                    yield return "multiplyThicknessWithAlpha";
                }

                if (materialType != MaterialType.SimpleLit && materialType != MaterialType.SimpleLitTranslucent)
                {
                    yield return "enableShadows";
                    yield return "enableSpecular";
                    yield return "enableTransmission";
                    yield return "enableCookie";
                    yield return "enableEnvLight";
                }

                if (!allowTextures)
                {
                    yield return "useBaseColorMap";
                    yield return "useMaskMap";
                    yield return "useNormalMap";
                    yield return "useEmissiveMap";
                    yield return "alphaMask";
                }

                if (GetOrRefreshShaderGraphObject() != null)
                {
                    yield return "materialType";
                    yield return "useEmissive";
                    yield return "colorMode";
                }
                else if ((colorMode & ColorMode.Emissive) != 0)
                    yield return "useEmissive";

                if (isBlendModeOpaque)
                {
                    yield return "onlyAmbientLighting";
                    yield return "preserveSpecularLighting";
                    yield return "excludeFromTAA";
                }
            }
        }

        public override void OnEnable()
        {
            colorMapping = ColorMappingMode.Default;
            base.OnEnable();
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
                forwardPassName.Write(GetOrRefreshShaderGraphObject() == null && (materialType == MaterialType.SimpleLit || materialType == MaterialType.SimpleLitTranslucent) ? "ForwardOnly" : "Forward");
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXHDRPForwardPassName}", forwardPassName);
            }
        }
    }
}
