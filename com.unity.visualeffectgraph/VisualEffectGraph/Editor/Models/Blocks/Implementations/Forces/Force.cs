using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class Force : VFXBlock
    {
        public enum ForceMode
        {
            Absolute,
            Relative
        }

        [VFXSetting]
        public ForceMode Mode = ForceMode.Absolute;

        public override string name { get { return "Force"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this))
                    yield return p;

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
            }
        }

        public class InputProperties
        {
            [Tooltip("Force Vector applied to Particle Velocity (in squared units per second)")]
            public Vector3 Force = new Vector3(0.0f, -9.81f, 0.0f);
        }

        public override string source
        {
            get
            {
                string forceVector = "0.0";
                switch (Mode)
                {
                    case ForceMode.Absolute: forceVector = "Force"; break;
                    case ForceMode.Relative: forceVector = "(Force - velocity)"; break;
                }

                return "velocity += " + forceVector + " * deltaTime;";
            }
        }
    }
}
