using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractParticleHDRPLitOutput : VFXAbstractParticleOutput
    {
        public enum MaterialType
        {
            Standard,
            SpecularColor,
            Translucent,
        }

        private readonly string[] kMaterialTypeToName = new string[] {
            "StandardProperties",
            "SpecularColorProperties",
            "TranslucentProperties",
        };

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Lighting")]
        protected MaterialType materialType = MaterialType.Standard;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Range(1, 15)]
        protected uint diffusionProfile = 1;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useBaseColorMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useMaskMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useNormalMap = false;

        protected virtual bool allowTextures { get { return true; }}

        public class HDRPLitInputProperties
        {
            [Range(0, 1)]
            public float smoothness = 0.5f;
        }

        public class StandardProperties
        {
            [Range(0, 1)]
            public float metallic = 0.5f;
        }

        public class SpecularColorProperties
        {
            public Color specularColor = Color.gray;
        }

        public class TranslucentProperties
        {
            [Range(0, 1)]
            public float thickness = 1.0f;
        }

        public class BaseColorMapProperties
        {
            [Tooltip("Base Color (RGB) Opacity (A)")]
            public Texture2D baseColor = VFXResources.defaultResources.particleTexture;
        }

        public class MaskMapProperties
        {
            [Tooltip("Metallic (R) AO (G) Smoothness (A)")]
            public Texture2D maskMap = VFXResources.defaultResources.noiseTexture;
        }

        public class NormalMapProperties
        {
            [Tooltip("Normal in tangent space")]
            public Texture2D normalMap;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                properties = properties.Concat(PropertiesFromType("HDRPLitInputProperties"));
                properties = properties.Concat(PropertiesFromType(kMaterialTypeToName[(int)materialType]));
                if (allowTextures)
                {
                    if (useBaseColorMap)
                        properties = properties.Concat(PropertiesFromType("BaseColorMapProperties"));
                    if (useMaskMap)
                        properties = properties.Concat(PropertiesFromType("MaskMapProperties"));
                    if (useNormalMap)
                        properties = properties.Concat(PropertiesFromType("NormalMapProperties"));
                }

                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            yield return slotExpressions.First(o => o.name == "smoothness");

            switch (materialType)
            {
                case MaterialType.Standard:
                    yield return slotExpressions.First(o => o.name == "metallic");
                    break;

                case MaterialType.SpecularColor:
                    yield return slotExpressions.First(o => o.name == "specularColor");
                    break;

                case MaterialType.Translucent:
                    yield return slotExpressions.First(o => o.name == "thickness");
                    yield return new VFXNamedExpression(VFXValue.Constant(diffusionProfile), "diffusionProfile");
                    break;

                default: break;
            }

            if (allowTextures)
            {
                if (useBaseColorMap)
                    yield return slotExpressions.First(o => o.name == "baseColor");
                if (useMaskMap)
                    yield return slotExpressions.First(o => o.name == "maskMap");
                if (useNormalMap)
                    yield return slotExpressions.First(o => o.name == "normalMap");
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

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
                        break;

                    default: break;
                }

                if (allowTextures)
                {
                    if (useBaseColorMap)
                        yield return "HDRP_USE_BASE_COLOR_MAP";
                    if (useMaskMap)
                        yield return "HDRP_USE_MASK_MAP";
                    if (useNormalMap)
                        yield return "HDRP_USE_NORMAL_MAP";
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                if (materialType != MaterialType.Translucent)
                    yield return "diffusionProfile";

                if (!allowTextures)
                {
                    yield return "useBaseColorMap";
                    yield return "useMaskMap";
                    yield return "useNormalMap";
                }
            }
        }
    }
}
