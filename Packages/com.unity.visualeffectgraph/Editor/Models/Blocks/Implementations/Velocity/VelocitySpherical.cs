using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class VelocitySphericalVariantProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            foreach (var mode in Enum.GetValues(typeof(AttributeCompositionMode)).Cast<AttributeCompositionMode>())
            {
                // Skip the composition mode from main provider
                if (mode == AttributeCompositionMode.Overwrite)
                    continue;

                var composition = VFXBlockUtility.GetNameString(mode);

                yield return new Variant(
                    $"{composition} Spherical Velocity from Direction & Speed",
                    null,
                    typeof(VelocitySpherical),
                    new[]
                    {
                        new KeyValuePair<string, object>("composition", mode),
                    });
            }
        }
    }

    class VelocitySphericalProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                "Set".Label().AppendLiteral("Velocity from Direction & Speed").AppendLabel("Spherical"),
                VelocityBase.Category,
                typeof(VelocitySpherical),
                new[]
                {
                    new KeyValuePair<string, object>("composition", AttributeCompositionMode.Overwrite),
                },
                () => new VelocitySphericalVariantProvider());
        }
    }

    [VFXInfo(experimental = true, variantProvider = typeof(VelocitySphericalProvider))]
    class VelocitySpherical : VelocityBase
    {
        public override string name => base.name.AppendLabel("Spherical");
        protected override bool altersDirection => true;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                foreach (var attribute in base.attributes)
                    yield return attribute;

                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            [Tooltip("Sets the center of the spherical direction. Particles will move outwards from this position.")]
            public Vector3 center;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var property in PropertiesFromType("InputProperties"))
                    yield return property;

                foreach (var property in base.inputProperties)
                    yield return property;
            }
        }

        public override string source
        {
            get
            {
                string outSource = "float3 sphereDirection = VFXSafeNormalize(position - center);\n";
                outSource += speedComputeString + "\n";
                outSource += string.Format(directionFormatBlendSource, "sphereDirection") + "\n";
                outSource += string.Format(velocityComposeFormatString, "direction * speed");
                return outSource;
            }
        }
    }
}
