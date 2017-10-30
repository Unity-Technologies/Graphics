using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Color")]
    class ColorOverLife : VFXBlock
    {
        public enum ColorApplicationMode
        {
            Color = 1 << 0,
            Alpha = 1 << 1,
            ColorAndAlpha = Color | Alpha,
        }
        [VFXSetting]
        public ColorApplicationMode mode = ColorApplicationMode.ColorAndAlpha;

        public override string name { get { return "Color over Life"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);

                if ((mode & ColorApplicationMode.Color) != 0)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Write);
                if ((mode & ColorApplicationMode.Alpha) != 0)
                    yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Write);
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
                string outSource = @"
float4 sampledColor = SampleGradient(gradient, age/lifetime);
";
                if ((mode & ColorApplicationMode.Color) != 0) outSource += "color = sampledColor.rgb;\n";
                if ((mode & ColorApplicationMode.Alpha) != 0) outSource += "alpha = sampledColor.a;\n";

                return outSource;
            }
        }
    }
}
