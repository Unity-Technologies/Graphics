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

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                properties = properties.Concat(PropertiesFromType("HDRPLitInputProperties"));
                properties = properties.Concat(PropertiesFromType(kMaterialTypeToName[(int)materialType]));

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
            }
        }
    }
}
