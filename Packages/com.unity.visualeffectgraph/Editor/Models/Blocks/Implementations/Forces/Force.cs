using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class Force : VFXBlock
    {
        [VFXSetting, SerializeField, Tooltip("Specifies whether the added force is relative to the current particle velocity or is an absolute value.")]
        ForceMode Mode = ForceMode.Absolute;

        public override string name { get { return "Force"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.Update; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public class AbsoluteProperties
        {
            [Tooltip("Sets the force vector applied to particles (in units per squared second).")]
            public Vector Force = new Vector3(1.0f, 0.0f, 0.0f);
        }

        public class RelativeProperties
        {
            [Tooltip("Sets the relative velocity affecting the particles.")]
            public Vector Velocity = new Vector3(1.0f, 0.0f, 0.0f);
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                return ForceHelper.attributes;
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var input in GetExpressionsFromSlots(this))
                    yield return input;

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (Mode == ForceMode.Absolute)
                    return PropertiesFromType("AbsoluteProperties");
                else
                    return PropertiesFromType("RelativeProperties").Concat(PropertiesFromType(typeof(ForceHelper.DragProperties)));
            }
        }

        public override string source
        {
            get
            {
                return string.Format("velocity += {0};", ForceHelper.ApplyForceString(Mode, Mode == ForceMode.Absolute ? "Force" : "Velocity"));
            }
        }
    }
}
