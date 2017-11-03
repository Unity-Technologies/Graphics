using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "FlipBook")]
    class FlipbookSetTexIndex : VFXBlock
    {
        // TODO: Let's factorize this this into a utility class
        public enum Mode
        {
            Constant,
            Random,
            CurveOverLife
        }

        [VFXSetting]
        public Mode mode = Mode.Random;

        public override string name { get { return "Flipbook Set TexIndex"; } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Write);
                if (mode == Mode.Random)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                }
                if (mode == Mode.CurveOverLife)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
                }
            }
        }
        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                switch (mode)
                {
                    case Mode.Constant: return PropertiesFromType("InputPropertiesConstant");
                    case Mode.Random: return PropertiesFromType("InputPropertiesRandom");
                    case Mode.CurveOverLife: return PropertiesFromType("InputPropertiesCurveOverLife");
                    default: throw new InvalidOperationException();
                }
            }
        }

        public class InputPropertiesConstant
        {
            [Tooltip("Frame index to set")]
            public float Value = 0.0f;
        }

        public class InputPropertiesRandom
        {
            [Tooltip("Minimum Frame index to set")]
            public float MinValue = 0.0f;
            [Tooltip("Maximum Frame index to set")]
            public float MaxValue = 15.0f;
        }

        public class InputPropertiesCurveOverLife
        {
            [Tooltip("Frame index to set over particle relative lifetime")]
            public AnimationCurve Curve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 15.0f) });
        }

        // Source code is the actual code of your nodeblock where you can access properties, attributes and optionally parameters.
        public override string source
        {
            get
            {
                string value = "";
                switch (mode)
                {
                    case Mode.Constant: value = "Value"; break;
                    case Mode.Random: value = "lerp(MinValue,MaxValue,RAND)"; break;
                    case Mode.CurveOverLife: value = "SampleCurve(Curve, age/lifetime)"; break;
                }

                string outSource = string.Format("texIndex = {0};", value);
                return outSource;
            }
        }
    }
}
