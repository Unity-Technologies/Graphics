using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block.Test
{
    [VFXInfo(category = "Tests")]
    class IncreaseLife : VFXBlock
    {
        public override string name { get { return "IncreaseLife"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kAll; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                return new List<VFXAttributeInfo>() { new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.ReadWrite) };
            }
        }

        public class InputProperties
        {
            public float increaseValue = 1.0f;
        }

        public override string source
        {
            get
            {
                return "lifetime += increaseValue;";
            }
        }
    }
}
