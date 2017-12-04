using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block.Test
{
    [VFXInfo(category = "Tests")]
    class GPUEventOnDie : VFXBlock
    {
        public override string name { get { return "GPU Event On Die"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.EventCount, VFXAttributeMode.ReadWrite);
            }
        }
        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }


        public class InputProperties
        {
            //None
        }

        public class OutputProperties
        {
            public GPUEvent evt;
        }

        public override string source
        {
            get
            {
                return
                    @"if (age + deltaTime > lifetime)
{
    eventCount += 1u;
}
";
            }
        }
    }
}
