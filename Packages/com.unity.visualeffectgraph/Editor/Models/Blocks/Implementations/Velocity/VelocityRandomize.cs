using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    class VelocityRandomVariantProvider : VariantProvider
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
                    composition.Label().AppendLiteral("Random Velocity from Direction & Speed"),
                    null,
                    typeof(VelocityRandomize),
                    new[]
                    {
                        new KeyValuePair<string, object>("composition", mode),
                    });
            }
        }
    }

    class VelocityRandomProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                "Set".Label().AppendLiteral("Velocity from Direction & Speed").AppendLabel("Random Direction"),
                VelocityBase.Category,
                typeof(VelocityRandomize),
                new[]
                {
                    new KeyValuePair<string, object>("composition", AttributeCompositionMode.Overwrite),
                },
                () => new VelocityRandomVariantProvider());
        }
    }

    [VFXInfo(experimental = true, variantProvider = typeof(VelocityRandomProvider))]
    class VelocityRandomize : VelocityBase
    {
        public override string name => base.name.AppendLabel("Random Direction");
        protected override bool altersDirection => true;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                foreach (var attribute in base.attributes)
                    yield return attribute;

                // we need to add seed only if it's not already present
                if (speedMode == SpeedMode.Constant)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
            }
        }

        public override string source
        {
            get
            {
                string outSource = "float3 randomDirection = normalize(RAND3 * 2.0f - 1.0f);\n";
                outSource += speedComputeString + "\n";
                outSource += string.Format(directionFormatBlendSource, "randomDirection") + "\n";
                outSource += string.Format(velocityComposeFormatString, "direction * speed");
                return outSource;
            }
        }
    }
}
