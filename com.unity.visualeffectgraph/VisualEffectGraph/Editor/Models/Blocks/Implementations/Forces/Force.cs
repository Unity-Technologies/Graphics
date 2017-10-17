using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.BlockLibrary
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
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWriteCurrent);
            }
        }

        public class InputProperties
        {
            public Vector Force = new Vector(0.0f, -9.81f, 0.0f);
        }

        public override string source
        {
            get
            {
                string forceVector = "0.0";

                switch (Mode)
                {
                    case ForceMode.Absolute: forceVector = "Force_vector"; break;
                    case ForceMode.Relative: forceVector = "(Force_vector - velocity)"; break;
                }

                return "velocity += " + forceVector + " * deltaTime;";
            }
        }
    }
}
