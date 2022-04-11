using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class VelocityBaseProvider : VariantProvider
    {
        public override IEnumerable<Variant> ComputeVariants()
        {
            return Enum.GetValues(typeof(AttributeCompositionMode))
                .OfType<AttributeCompositionMode>()
                .Select(x => new Variant(new[] { new KeyValuePair<string, object>("composition", x) }, new[] { "velocity", VFXBlockUtility.GetNameString(x) }));
        }
    }

    abstract class VelocityBase : VFXBlock
    {
        public enum SpeedMode
        {
            Constant,
            Random
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies what operation to perform on the velocity attribute. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        protected AttributeCompositionMode composition = AttributeCompositionMode.Add;

        [VFXSetting, SerializeField, Tooltip("Specifies whether the applied speed is constant or random.")]
        protected SpeedMode speedMode = SpeedMode.Constant;

        public override VFXContextType compatibleContexts { get { return VFXContextType.InitAndUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public override string name { get { return string.Format("{0} Velocity from Direction & Speed ({1})", VFXBlockUtility.GetNameString(composition), "{0}"); } }

        protected abstract bool altersDirection { get; }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                string speedInputPropertiesClass = speedMode == SpeedMode.Constant ? "InputPropertiesSpeedConstant" : "InputPropertiesSpeedRandom";

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
            [Tooltip("Sets the speed by which the particles will move in the new direction.")]
            public float Speed = 1.0f;
        }

        public class InputPropertiesSpeedRandom
        {
            [Tooltip("Sets the minimum speed by which the particles will move in the new direction.")]
            public float MinSpeed = 0.0f;

            [Tooltip("Sets the maximum speed by which the particles will move in the new direction.")]
            public float MaxSpeed = 1.0f;
        }

        public class InputPropertiesDirectionBlend
        {
            [Range(0, 1), Tooltip("Controls the blend between the original emission direction and the new direction.")]
            public float DirectionBlend = 1.0f;
        }

        public class InputPropertiesVelocityBlend
        {
            [Range(0, 1), Tooltip("Controls the blend between the original velocity and the newly computed velocity.")]
            public float VelocityBlend = 1.0f;
        }

        protected string directionFormatBlendSource
        {
            get { return "direction = VFXSafeNormalize(lerp(direction, {0}, DirectionBlend));"; }
        }

        protected string speedComputeString
        {
            get
            {
                switch (speedMode)
                {
                    case SpeedMode.Constant: return "float speed = Speed;";
                    case SpeedMode.Random: return "float speed = lerp(MinSpeed,MaxSpeed,RAND);";
                    default: throw new NotImplementedException("Unimplemented random mode: " + speedMode);
                }
            }
        }

        protected string velocityComposeFormatString
        {
            get { return VFXBlockUtility.GetComposeString(composition, "velocity", "{0}", "VelocityBlend"); }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, composition == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Direction, altersDirection ? VFXAttributeMode.ReadWrite : VFXAttributeMode.Read);

                if (speedMode != SpeedMode.Constant)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
            }
        }
    }
}
