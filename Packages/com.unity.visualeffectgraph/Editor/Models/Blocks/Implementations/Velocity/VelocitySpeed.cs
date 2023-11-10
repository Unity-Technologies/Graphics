using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    class VelocitySpeedVariantProvider : VariantProvider
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
                    $"{composition} Velocity from Direction & Speed",
                    null,
                    typeof(VelocitySpeed),
                    new[]
                    {
                        new KeyValuePair<string, object>("composition", mode),
                    });
            }
        }
    }

    class VelocitySpeedProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                $"Set Velocity from Direction & Speed",
                "Attribute/From Direction & Speed",
                typeof(VelocitySpeed),
                new[]
                {
                    new KeyValuePair<string, object>("composition", AttributeCompositionMode.Overwrite),
                },
                () => new VelocitySpeedVariantProvider());
        }
    }

    [VFXInfo(experimental = true, variantProvider = typeof(VelocitySpeedProvider))]
    class VelocitySpeed : VelocityBase
    {
        public override string name { get { return string.Format(base.name, "Change Speed"); } }
        protected override bool altersDirection { get { return false; } }

        public override string source
        {
            get
            {
                string outSource = speedComputeString + "\n";
                outSource += string.Format(velocityComposeFormatString, "direction * speed");
                return outSource;
            }
        }
    }
}
