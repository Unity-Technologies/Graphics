using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.VFX.HDRP
{
    abstract class VFXAbstractParticleHDRPOutput : VFXShaderGraphParticleOutput
    {
        [Flags]
        public enum ColorMode
        {
            None = 0,
            BaseColor = 1 << 0,
            Emissive = 1 << 1,
            BaseColorAndEmissive = BaseColor | Emissive,
        }

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
        protected VFXAbstractParticleHDRPOutput(bool strip = false) : base(strip) { }

        protected virtual bool allowTextures { get { return GetOrRefreshShaderGraphObject() == null; } }
        protected virtual bool useNormalScale => true;

        protected IEnumerable<VFXPropertyWithValue> baseColorMapProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), "baseColorMap", new TooltipAttribute("Specifies the base color (RGB) and opacity (A) of the particle.")), (usesFlipbook ? null : VFXResources.defaultResources.particleTexture));
            }
        }

        protected IEnumerable<VFXPropertyWithValue> maskMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), "maskMap", new TooltipAttribute("Specifies the Mask Map for the particle - Metallic (R), Ambient occlusion (G), and Smoothness (A).")), (usesFlipbook ? null : VFXResources.defaultResources.maskTexture));
            }
        }
        protected IEnumerable<VFXPropertyWithValue> normalMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), "normalMap", new TooltipAttribute("Specifies the Normal map to obtain normals in tangent space for the particle.")), (usesFlipbook ? null : VFXResources.defaultResources.normalTexture));
                if(useNormalScale)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "normalScale", new TooltipAttribute("Sets the scale of the normals. Larger values increase the impact of the normals.")), 1.0f);
            }
        }
        protected IEnumerable<VFXPropertyWithValue> emissiveMapsProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetTextureType(), "emissiveMap", new TooltipAttribute("Specifies the Emissive map (RGB) used to make particles glow.")));
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "emissiveScale", new TooltipAttribute("Sets the scale of the emission obtained from the emissive map.")), 1.0f);
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

        public override sealed bool CanBeCompiled()
        {
            return (VFXLibrary.currentSRPBinder is VFXHDRPBinder) && base.CanBeCompiled();
        }

        protected virtual bool useEmissiveColor { get { return useEmissive; } }
        protected override bool needsExposureWeight { get { return GetOrRefreshShaderGraphObject() == null && ((colorMode & ColorMode.Emissive) != 0 || useEmissive || useEmissiveMap); } }

        protected override bool bypassExposure { get { return false; } }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;

                if (GetOrRefreshShaderGraphObject() == null)
                {
                    if (allowTextures)
                    {
                        if (useBaseColorMap != BaseColorMapMode.None)
                            properties = properties.Concat(baseColorMapProperties);
                    }

                    if ((colorMode & ColorMode.BaseColor) == 0) // particle color is not used as base color so add a slot
                        properties = properties.Concat(PropertiesFromType("BaseColorProperties"));

                    if (allowTextures)
                    {
                        if (useMaskMap)
                            properties = properties.Concat(maskMapsProperties);
                        if (useNormalMap)
                            properties = properties.Concat(normalMapsProperties);
                        if (useEmissiveMap)
                            properties = properties.Concat(emissiveMapsProperties);
                    }

                    if (((colorMode & ColorMode.Emissive) == 0) && useEmissiveColor)
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
                if (allowTextures)
                {
                    if (useBaseColorMap != BaseColorMapMode.None)
                        yield return slotExpressions.First(o => o.name == "baseColorMap");
                    if (useMaskMap)
                        yield return slotExpressions.First(o => o.name == "maskMap");
                    if (useNormalMap)
                    {
                        yield return slotExpressions.First(o => o.name == "normalMap");
                        if(useNormalScale)
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

                if (((colorMode & ColorMode.Emissive) == 0) && useEmissiveColor)
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
                    else if (useEmissiveColor)
                        yield return "HDRP_USE_ADDITIONAL_EMISSIVE_COLOR";
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return "colorMapping";

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
                    yield return "excludeFromTUAndAA";
                }
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

        protected override IEnumerable<string> untransferableSettings
        {
            get
            {
                foreach (var setting in base.untransferableSettings)
                {
                    yield return setting;
                }
                yield return "colorMapping";
            }
        }

    }
}
