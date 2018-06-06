using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    abstract class VelocityBase : VFXBlock
    {
        public enum SpeedRandomMode
        {
            Constant,
            Random
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected AttributeCompositionMode composition = AttributeCompositionMode.Add;

        [VFXSetting, SerializeField]
        protected SpeedRandomMode speedRandomMode = SpeedRandomMode.Constant;

        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override string name { get { return string.Format("{0} Velocity ({1})", VFXBlockUtility.GetNameString(composition), "{0}"); } }

        protected abstract bool altersDirection { get; }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                string speedInputPropertiesClass = speedRandomMode == SpeedRandomMode.Constant ? "InputPropertiesSpeedConstant" : "InputPropertiesSpeedRandom";

                foreach (var property in PropertiesFromType(speedInputPropertiesClass))
                    yield return property;

                if (altersDirection)
                {
                    foreach (var property in PropertiesFromType("InputPropertiesDirectionBlend"))
                        yield return property;
                }

                if (composition == AttributeCompositionMode.Blend)
                {
                    foreach (var property in PropertiesFromType("InputPropertiesVelocityBlend"))
                        yield return property;
                }
            }
        }

        public class InputPropertiesSpeedConstant
        {
            [Tooltip("The speed to compute for the particles, in the new direction.")]
            public float Speed = 1.0f;
        }

        public class InputPropertiesSpeedRandom
        {
            [Tooltip("The minimum speed to compute for the particles, in the new direction.")]
            public float MinSpeed = 0.0f;

            [Tooltip("The minimum speed to compute for the particles, in the new direction.")]
            public float MaxSpeed = 1.0f;
        }

        public class InputPropertiesDirectionBlend
        {
            [Range(0, 1), Tooltip("Blend between the original emission direction and the new direction, based on this value.")]
            public float DirectionBlend = 1.0f;
        }

        public class InputPropertiesVelocityBlend
        {
            [Range(0, 1), Tooltip("Blend factor between the original velocity and the newly computed velocity.")]
            public float VelocityBlend = 1.0f;
        }

        public string directionFormatBlendSource
        {
            get { return "direction = normalize(lerp(direction, {0}, DirectionBlend));"; }
        }

        public string speedComputeString
        {
            get
            {
                switch (speedRandomMode)
                {
                    case SpeedRandomMode.Constant: return "float speed = Speed;";
                    case SpeedRandomMode.Random: return "float speed = lerp(MinSpeed,MaxSpeed,RAND);";
                    default: throw new NotImplementedException("Unimplemented random mode: " + speedRandomMode);
                }
            }
        }

        public string velocityComposeFormatString
        {
            get { return VFXBlockUtility.GetComposeString(composition, "velocity", "{0}", "VelocityBlend"); }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, composition == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Direction, VFXAttributeMode.ReadWrite);

                if (speedRandomMode != SpeedRandomMode.Constant)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
            }
        }
    }
}
