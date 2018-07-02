using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    //[VFXInfo(category = "Time")] deprecated
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
            public float CurveSample = 0;
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

        public override void Sanitize()
        {
            Debug.Log("Sanitizing Graph: Automatically replace SetLifetime with corresponding generic blocks");

            if (mode == SetMode.Constant || mode == SetMode.Random)
            {
                var newBlock = CreateInstance<SetAttribute>();
                newBlock.SetSettingValue("attribute", "lifetime");
                newBlock.SetSettingValue("Random", mode == SetMode.Constant ? RandomMode.Off : RandomMode.Uniform);

                // Transfer links
                VFXSlot.CopyLinksAndValue(newBlock.GetInputSlot(0), GetInputSlot(0), true);
                if (mode == SetMode.Random)
                    VFXSlot.CopyLinksAndValue(newBlock.GetInputSlot(1), GetInputSlot(1), true);
                ReplaceModel(newBlock, this);
            }
            else // Ignore random curve
            {
                var newBlock = CreateInstance<AttributeFromCurve>();
                newBlock.SetSettingValue("attribute", "lifetime");

                // Transfer links
                VFXSlot.CopyLinksAndValue(newBlock.GetInputSlot(0), GetInputSlot(0), true);
                ReplaceModel(newBlock, this);
            }
        }
    }
}
