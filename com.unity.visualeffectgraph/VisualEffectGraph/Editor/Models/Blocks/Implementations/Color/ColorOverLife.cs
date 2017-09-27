using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.BlockLibrary
{
    [VFXInfo(category = "Color")]
    class ColorOverLife : VFXBlock
    {
        public enum ColorApplicationMode
        {
            ColorAndAlpha,
            Color,
            Alpha,
        }
        [VFXSetting]
        public ColorApplicationMode Mode;

        public override string name { get { return "Color over Life"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);

                if (Mode == ColorApplicationMode.Color || Mode == ColorApplicationMode.ColorAndAlpha)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.ReadWrite);
                if (Mode == ColorApplicationMode.Alpha || Mode == ColorApplicationMode.ColorAndAlpha)
                    yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.ReadWrite);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var gradient = GetExpressionsFromSlots(this).First(o => o.name == "gradient");
                var t = new VFXAttributeExpression(VFXAttribute.Age) / new VFXAttributeExpression(VFXAttribute.Lifetime);
                var sampled = new VFXExpressionSampleGradient(gradient.exp, t);
                yield return new VFXNamedExpression(sampled, "sampledColor");
            }
        }

        public class InputProperties
        {
            public Gradient gradient;
        }

        public override string source
        {
            get
            {
                string outSource = @"";

                if (Mode == ColorApplicationMode.Color || Mode == ColorApplicationMode.ColorAndAlpha)
                    outSource += "color = sampledColor.rgb;\n";
                if (Mode == ColorApplicationMode.Alpha || Mode == ColorApplicationMode.ColorAndAlpha)
                    outSource += "alpha = sampledColor.a;\n";

                return outSource;
            }
        }
    }
}
