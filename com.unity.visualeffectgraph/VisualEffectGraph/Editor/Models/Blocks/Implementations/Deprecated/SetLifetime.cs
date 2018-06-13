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
            FromCurveRandom,
        }

        [VFXSetting]
        public SetMode mode = SetMode.Random;

        public override string name { get { return "Set LifeTime : " + mode.ToString(); } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInit; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Write);
                if (mode == SetMode.Random || mode == SetMode.FromCurveRandom)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
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
                    case SetMode.FromCurveRandom: return PropertiesFromType("InputPropertiesCurveRandom");
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
            public float Min = 0.5f;
            public float Max = 1.0f;
        }

        public class InputPropertiesCurve
        {
            public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            public float CurveSample;
        }
        public class InputPropertiesCurveRandom
        {
            public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
        public override string source
        {
            get
            {
                switch (mode)
                {
                    case SetMode.Constant:  return "lifetime = Value;";
                    case SetMode.Random:    return "lifetime = lerp(Min,Max,RAND);";
                    case SetMode.FromCurve: return "lifetime = SampleCurve(Curve, CurveSample);";
                    case SetMode.FromCurveRandom: return "lifetime = SampleCurve(Curve, RAND);";
                    default: throw new InvalidOperationException();
                }
            }
        }
    }
}
