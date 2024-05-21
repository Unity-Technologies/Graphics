using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class VelocityDirectionVariantProvider : VariantProvider
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
                    composition.Label().AppendLiteral("Velocity from Direction & Speed"),
                    null,
                    typeof(VelocityDirection),
                    new[]
                    {
                        new KeyValuePair<string, object>("composition", mode),
                    });
            }
        }
    }

    class VelocityDirectionProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                "Set".Label().AppendLiteral("Velocity from Direction & Speed").AppendLabel("New Direction"),
                VelocityBase.Category,
                typeof(VelocityDirection),
                new[]
                {
                    new KeyValuePair<string, object>("composition", AttributeCompositionMode.Overwrite),
                },
                () => new VelocityDirectionVariantProvider());
        }
    }

    [VFXInfo(experimental = true, variantProvider = typeof(VelocityDirectionProvider))]
    class VelocityDirection : VelocityBase
    {
        public override string name => base.name.AppendLabel("New Direction");
        protected override bool altersDirection => true;

        public class InputProperties
        {
            [Tooltip("Sets the direction in which particles should move.")]
            public DirectionType Direction = new DirectionType() { direction = Vector3.forward };
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
                string outSource = speedComputeString + "\n";
                outSource += string.Format(directionFormatBlendSource, "Direction") + "\n";
                outSource += string.Format(velocityComposeFormatString, "direction * speed");
                return outSource;
            }
        }
    }
}
