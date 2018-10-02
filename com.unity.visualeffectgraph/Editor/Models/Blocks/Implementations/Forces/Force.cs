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
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
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
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            [Tooltip("Force vector applied to particles (in units per squared second), in Relative mode the flow speed of the medium (eg: wind)")]
            public Vector3 Force = new Vector3(1.0f, 0.0f, 0.0f);
        }

        public override string source
        {
            get
            {
                string forceVector = "0.0";
                switch (Mode)
                {
                    case ForceMode.Absolute: forceVector = "(Force / mass) * deltaTime"; break;
                    case ForceMode.Relative: forceVector = "(Force - velocity) * min(1.0f,deltaTime / mass)"; break;
                }

                return "velocity += " + forceVector + ";";
            }
        }
    }
}
