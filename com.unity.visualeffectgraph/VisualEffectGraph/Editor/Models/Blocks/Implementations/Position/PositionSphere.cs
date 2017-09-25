using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.BlockLibrary
{
    [VFXInfo(category = "Position")]
    class PositionSphere : VFXBlock
    {
        // TODO: Let's factorize this this into a utility class
        public enum PrimitivePositionMode
        {
            Surface,
            Volume,
            ThicknessAbsolute,
            ThicknessRelative
        }

        [VFXSetting]
        public PrimitivePositionMode mode;
        [VFXSetting]
        public bool applySpeed;

        public override string name { get { return "Position: Sphere"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                if (applySpeed)
                    yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this))
                    yield return p;

                VFXExpression factor = VFXValue.Constant(0.0f);

                switch (mode)
                {
                    case PrimitivePositionMode.Surface:
                        factor = VFXValue.Constant(0.0f);
                        break;
                    case PrimitivePositionMode.Volume:
                        factor = VFXValue.Constant(1.0f);
                        break;
                    case PrimitivePositionMode.ThicknessAbsolute:
                    case PrimitivePositionMode.ThicknessRelative:
                        {
                            var thickness = GetInputSlot(2).GetExpression();
                            if (mode == PrimitivePositionMode.ThicknessAbsolute)
                            {
                                var radius = GetInputSlot(0)[1].GetExpression();
                                thickness = thickness / radius;
                            }
                            factor = VFXOperatorUtility.Clamp(thickness, VFXValue.Constant(0.0f), VFXValue.Constant(1.0f));
                            break;
                        }
                }

                yield return new VFXNamedExpression(new VFXExpressionPow(VFXValue.Constant(1.0f) - factor, VFXValue.Constant(3.0f)), "surfaceFactor");
            }
        }

        // TODO : Remove InputProperties and process yielding of VFXSlots depending on VFXSettings once available
        public class InputProperties
        {
            public Sphere Sphere = new Sphere() { radius = 1.0f };
            public float Speed = 1.0f;
            public float Thickness = 0.1f;
        }

        public override string source
        {
            get
            {
                string outSource = @"
float u1 = 2.0 * RAND - 1.0;
float u2 = UNITY_TWO_PI * RAND;
float u3 = pow(surfaceFactor + (1 - surfaceFactor) * RAND,1.0f/3.0f);
float3 pos = VFXPositionOnSphere(Sphere,u1,u2,u3);
position += pos;
";
                if (applySpeed)
                    outSource += "velocity += normalize(pos - Sphere_center) * Speed;";

                return outSource;
            }
        }


    }
}
