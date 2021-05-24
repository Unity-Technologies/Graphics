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

        private readonly string[] kMaterialTypeToName = new string[]
        {
            nameof(StandardProperties),
            nameof(SpecularColorProperties)
        };

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, FormerlySerializedAs("materialType"), Header("Lighting"), Tooltip("Specifies the surface type of this output. Surface types determine how the particle will react to light.")]
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

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, an emissive color field becomes available in the output to make particles glow.")]
        protected bool useEmissive = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the normals of the particle are inverted when seen from behind, allowing quads with culling set to off to receive correct lighting information.")]
        protected bool doubleSided = false;

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
                    properties = properties.Concat(PropertiesFromType(kMaterialTypeToName[(int)workflowMode]));

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

                switch (workflowMode)
                {
                    case WorkflowMode.Metallic:
                        yield return slotExpressions.First(o => o.name == nameof(StandardProperties.metallic));
                        break;
                    case WorkflowMode.Specular:
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
                    switch (workflowMode)
                    {
                        case WorkflowMode.Metallic:
                            yield return "URP_MATERIAL_TYPE_METALLIC";
                            break;

                        case WorkflowMode.Specular:
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
                    else if (useEmissive)
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

                if (GetOrRefreshShaderGraphObject() != null)
                {
                    yield return nameof(workflowMode);
                    yield return nameof(useEmissive);
                    yield return nameof(colorMode);
                    yield return nameof(smoothnessSource);
                }
                else if ((colorMode & ColorMode.Emissive) != 0)
                    yield return nameof(useEmissive);

                yield return nameof(excludeFromTAA);
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
                if (workflowMode == WorkflowMode.Specular)
                    forwardDefines.Write("#define _SPECULAR_SETUP");
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXURPForwardDefines}", forwardDefines);

                // URP GBuffer specific defines
                var gbufferDefines = new VFXShaderWriter();
                if (workflowMode == WorkflowMode.Specular)
                    gbufferDefines.Write("#define _SPECULAR_SETUP");
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXURPGBufferDefines}", gbufferDefines);
            }
        }
    }
}
#endif
