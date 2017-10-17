using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Time")]
    class SetLifetime : VFXBlock
    {
        // TODO: Let's factorize this this into a utility class
        public enum SetMode
        {
            Constant,
            Random,
            FromCurve,
        }

        [VFXSetting]
        public SetMode mode = SetMode.Random;

        public override string name { get { return "Set Lifetime"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInit; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.WriteCurrent);
                if (mode != SetMode.Constant)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWriteCurrent);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                switch (mode)
                {
                    case SetMode.Constant: return PropertiesFromType("InputPropertiesConstant");
                    case SetMode.Random: return PropertiesFromType("InputPropertiesRandom");
                    case SetMode.FromCurve: return PropertiesFromType("InputPropertiesCurve");
                    default: throw new InvalidOperationException();
                }
            }
        }

        public class InputPropertiesConstant
        {
            public float Value = 1.0f;
        }

        public class InputPropertiesRandom
        {
            public float MinValue = 0.5f;
            public float MaxValue = 1.0f;
        }

        public class InputPropertiesCurve
        {
            public AnimationCurve Curve;
        }

        public override string source
        {
            get
            {
                switch (mode)
                {
                    case SetMode.Constant:  return "lifetime = Value;";
                    case SetMode.Random:    return "lifetime = lerp(MinValue,MaxValue,RAND);";
                    case SetMode.FromCurve: return "lifetime = SampleCurve(Curve, RAND);";
                    default: throw new InvalidOperationException();
                }
            }
        }
    }
}
