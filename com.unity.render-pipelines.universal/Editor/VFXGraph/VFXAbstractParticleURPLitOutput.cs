#if HAS_VFX_GRAPH

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractParticleURPLitOutput : VFXShaderGraphParticleOutput
    {
        public enum MaterialType
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

        private readonly string[] kMaterialTypeToName = new string[]
        {
            nameof(StandardProperties),
            nameof(SpecularColorProperties)
        };

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Lighting"), Tooltip("Specifies the surface type of this output. Surface types determine how the particle will react to light.")]
        protected MaterialType materialType = MaterialType.Metallic;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles in this output are not affected by any lights in the scene and only receive ambient and light probe lighting.")]
        protected bool onlyAmbientLighting = false;

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

        //TODOPAUL : Check probably simple lit only features

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Simple Lit features"), Tooltip("When enabled, the particle will receive shadows.")]
        protected bool enableShadows = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles will receive specular highlights.")]
        protected bool enableSpecular = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles can be affected by light cookies.")]
        protected bool enableCookie = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles can be affected by environment light set in the global volume profile.")]
        protected bool enableEnvLight = true;

        protected VFXAbstractParticleURPLitOutput(bool strip = false) : base(strip) {}

        protected virtual bool allowTextures { get { return GetOrRefreshShaderGraphObject() == null; } }

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

        private static readonly string kBaseColorMap = "baseColorMap";
        protected IEnumerable<VFXPropertyWithValue> baseColorMapProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), kBaseColorMap, new TooltipAttribute("Specifies the base color (RGB) and opacity (A) of the particle.")), (usesFlipbook ? null : VFXResources.defaultResources.particleTexture));
            }
        }

        private static readonly string kMaskMap = "maskMap";
        protected IEnumerable<VFXPropertyWithValue> maskMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), kMaskMap, new TooltipAttribute("Specifies the Mask Map for the particle - Metallic (R), Ambient occlusion (G), and Smoothness (A).")), (usesFlipbook ? null : VFXResources.defaultResources.noiseTexture));
            }
        }

        private static readonly string kNormalMap = "normalMap";
        private static readonly string kNormalScale = "normalScale";
        protected IEnumerable<VFXPropertyWithValue> normalMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), kNormalMap, new TooltipAttribute("Specifies the Normal map to obtain normals in tangent space for the particle.")));
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

        protected override bool needsExposureWeight { get { return GetOrRefreshShaderGraphObject() == null && ((colorMode & ColorMode.Emissive) != 0 || useEmissive || useEmissiveMap); } }

        protected override bool bypassExposure { get { return false; } }

        protected override RPInfo currentRP => urpLitInfo;

        public override bool isLitShader => true;

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;

                if (GetOrRefreshShaderGraphObject() == null)
                {
                    properties = properties.Concat(PropertiesFromType(nameof(URPLitInputProperties)));
                    properties = properties.Concat(PropertiesFromType(kMaterialTypeToName[(int)materialType]));

                    if (allowTextures)
                    {
                        if (useBaseColorMap != BaseColorMapMode.None)
                            properties = properties.Concat(baseColorMapProperties);
                    }

                    if ((colorMode & ColorMode.BaseColor) == 0) // particle color is not used as base color so add a slot
                        properties = properties.Concat(PropertiesFromType(nameof(BaseColorProperties)));

                    if (allowTextures)
                    {
                        if (useMaskMap)
                            properties = properties.Concat(maskMapsProperties);
                        if (useNormalMap)
                            properties = properties.Concat(normalMapsProperties);
                        if (useEmissiveMap)
                            properties = properties.Concat(emissiveMapsProperties);
                    }

                    if (((colorMode & ColorMode.Emissive) == 0) && useEmissive)
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
                yield return slotExpressions.First(o => o.name == nameof(URPLitInputProperties.smoothness));

                switch (materialType)
                {
                    case MaterialType.Metallic:
                        yield return slotExpressions.First(o => o.name == nameof(StandardProperties.metallic));
                        break;
                    case MaterialType.Specular:
                        yield return slotExpressions.First(o => o.name == nameof(SpecularColorProperties.specularColor));
                        break;
                    default:
                        break;
                }

                if (allowTextures)
                {
                    if (useBaseColorMap != BaseColorMapMode.None)
                        yield return slotExpressions.First(o => o.name == kBaseColorMap);
                    if (useMaskMap)
                        yield return slotExpressions.First(o => o.name == kMaskMap);
                    if (useNormalMap)
                    {
                        yield return slotExpressions.First(o => o.name == kNormalMap);
                        yield return slotExpressions.First(o => o.name == kNormalScale);
                    }
                    if (useEmissiveMap)
                    {
                        yield return slotExpressions.First(o => o.name == kEmissiveMap);
                        yield return slotExpressions.First(o => o.name == kEmissiveScale);
                    }
                }

                if ((colorMode & ColorMode.BaseColor) == 0)
                    yield return slotExpressions.First(o => o.name == nameof(BaseColorProperties.baseColor));

                if (((colorMode & ColorMode.Emissive) == 0) && useEmissive)
                    yield return slotExpressions.First(o => o.name == nameof(EmissiveColorProperties.emissiveColor));
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
                    switch (materialType)
                    {
                        case MaterialType.Metallic:
                            yield return "URP_MATERIAL_TYPE_METALLIC";
                            break;

                        case MaterialType.Specular:
                            yield return "URP_MATERIAL_TYPE_SPECULAR";
                            break;
                    }

                if (allowTextures)
                {
                    if (useBaseColorMap != BaseColorMapMode.None)
                        yield return "URP_USE_BASE_COLOR_MAP";
                    if ((useBaseColorMap & BaseColorMapMode.Color) != 0)
                        yield return "URP_USE_BASE_COLOR_MAP_COLOR";
                    if ((useBaseColorMap & BaseColorMapMode.Alpha) != 0)
                        yield return "URP_USE_BASE_COLOR_MAP_ALPHA";
                    if (useMaskMap)
                        yield return "URP_USE_MASK_MAP";
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
                    else if (useEmissive)
                        yield return "URP_USE_ADDITIONAL_EMISSIVE_COLOR";
                }

                if (doubleSided)
                    yield return "USE_DOUBLE_SIDED";

                if (onlyAmbientLighting && !isBlendModeOpaque)
                    yield return "USE_ONLY_AMBIENT_LIGHTING";

                if (isBlendModeOpaque)
                    yield return "IS_OPAQUE_NOT_SIMPLE_LIT_PARTICLE";
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
                    yield return nameof(useMaskMap);
                    yield return nameof(useNormalMap);
                    yield return nameof(useEmissiveMap);
                    //yield return nameof(alphaMask); //TODOPAUL There isn't alphaMask
                }

                if (GetOrRefreshShaderGraphObject() != null)
                {
                    yield return nameof(materialType);
                    yield return nameof(useEmissive);
                    yield return nameof(colorMode);
                }
                else if ((colorMode & ColorMode.Emissive) != 0)
                    yield return nameof(useEmissive);

                if (isBlendModeOpaque)
                {
                    yield return nameof(onlyAmbientLighting);
                    yield return nameof(preserveSpecularLighting);
                    yield return nameof(excludeFromTAA);
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

                // URP Forward specific defines
                var forwardDefines = new VFXShaderWriter();
                //Nothing for now
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXURPForwardDefines}", forwardDefines);
            }
        }
    }
}
#endif
