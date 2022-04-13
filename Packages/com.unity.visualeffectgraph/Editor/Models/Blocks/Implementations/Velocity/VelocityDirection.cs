using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute/{0}/Direction & Speed/{1}", experimental = true, variantProvider = typeof(VelocityBaseProvider))]
    class VelocityDirection : VelocityBase
    {
        public override string name { get { return string.Format(base.name, "New Direction"); } }

        protected override bool altersDirection { get { return true; } }

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
