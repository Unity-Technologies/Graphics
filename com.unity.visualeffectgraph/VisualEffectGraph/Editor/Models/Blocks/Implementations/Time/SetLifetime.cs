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
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
            }
        }

        // TODO : Remove InputProperties and process yielding of VFXSlots depending on VFXSettings once available
        public class InputProperties
        {
            public float Value = 1.0f;
            public float Other = 0.2f;
            public AnimationCurve Curve;
        }

        public override string source
        {
            get
            {
                string outSource = "";
                switch (mode)
                {
                    case SetMode.Constant: outSource = "lifetime = Value;"; break;
                    case SetMode.Random: outSource = "lifetime = lerp(Value,Other,RAND);"; break;
                    case SetMode.FromCurve: outSource = "lifetime = SampleCurve(Curve, RAND);"; break;
                }
                return outSource;
            }
        }
    }
}
