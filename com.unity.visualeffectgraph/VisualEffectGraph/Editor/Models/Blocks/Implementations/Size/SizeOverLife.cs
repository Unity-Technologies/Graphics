using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.BlockLibrary
{
    [VFXInfo(category = "Size")]
    class SizeOverLife : VFXBlock
    {
        public enum Composition
        {
            Overwrite,
            Scale
        }

        [VFXSetting]
        public Composition composition;

        public override string name { get { return "Size over Life"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);

                if (composition == Composition.Overwrite)
                    yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Write);
                if (composition == Composition.Scale)
                    yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.ReadWrite);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var curve = GetExpressionsFromSlots(this).First(o => o.name == "curve");
                var t = new VFXAttributeExpression(VFXAttribute.Age) / new VFXAttributeExpression(VFXAttribute.Lifetime);
                var sampled = new VFXExpressionSampleCurve(curve.exp, t);
                yield return new VFXNamedExpression(sampled, "sampledCurve");
            }
        }

        public class InputProperties
        {
            public AnimationCurve curve;
        }

        public override string source
        {
            get
            {
                switch (composition)
                {
                    case Composition.Overwrite: return "size = sampledCurve;";
                    case Composition.Scale: return "size *= sampledCurve;";
                }
                return "";
            }
        }
    }
}
