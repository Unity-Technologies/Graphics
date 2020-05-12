using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    abstract class CollisionBase : VFXBlock
    {
        public enum Mode
        {
            Solid,
            Inverted
        }

        public enum RadiusMode
        {
            None,
            FromSize,
            Custom,
        }

        [VFXSetting, Tooltip("Specifies the collision shape mode. The collider can either be a solid volume which particles cannot enter, or an empty volume which particles cannot leave.")]
        public Mode mode = Mode.Solid;
        [VFXSetting, Tooltip("Specifies the collision radius of each particle. This can be set to none (zero), automatically inherited from the particle size, or a custom value.")]
        public RadiusMode radiusMode = RadiusMode.None;
        [VFXSetting, Tooltip("When enabled, randomness is added to the direction in which particles bounce back to simulate collision with a rough surface.")]
        public bool roughSurface = false;

        public override VFXContextType compatibleContexts { get { return VFXContextType.Update; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
                if (roughSurface)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                // No need for size attributes here, they are explicitely used in collision parameters
            }
        }

        protected virtual bool allowInvertedCollision { get { return true; } }

        protected IEnumerable<VFXNamedExpression> collisionParameters
        {
            get
            {
                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");

                if (allowInvertedCollision)
                    yield return new VFXNamedExpression(VFXValue.Constant(mode == Mode.Solid ? 1.0f : -1.0f), "colliderSign");

                if (radiusMode == RadiusMode.None)
                    yield return new VFXNamedExpression(VFXValue.Constant(0.0f), "radius");
                else if (radiusMode == RadiusMode.FromSize)
                {
                    VFXExpression uniformSizeExp = new VFXAttributeExpression(VFXAttribute.Size);
                    VFXExpression maxSizeExp = new VFXAttributeExpression(VFXAttribute.ScaleX);
                    maxSizeExp = new VFXExpressionMax(new VFXAttributeExpression(VFXAttribute.ScaleY), maxSizeExp);
                    maxSizeExp = new VFXExpressionMax(new VFXAttributeExpression(VFXAttribute.ScaleZ), maxSizeExp);
                    maxSizeExp *= uniformSizeExp;
                    maxSizeExp *= VFXValue.Constant(0.5f);
                    yield return new VFXNamedExpression(maxSizeExp, "radius");
                }
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this))
                {
                    yield return p;
                }

                foreach (var p in collisionParameters)
                    yield return p;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (!allowInvertedCollision)
                    yield return "mode";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = PropertiesFromType(GetInputPropertiesTypeName());
                properties = properties.Concat(PropertiesFromType("CollisionProperties"));
                if (roughSurface)
                    properties = properties.Concat(PropertiesFromType("RoughnessProperties"));
                if (radiusMode == RadiusMode.Custom)
                    properties = properties.Concat(PropertiesFromType("RadiusProperties"));
                return properties;
            }
        }

        protected static string roughSurfaceSource
        {
            get
            {
                return @"
    float3 randomNormal = normalize(RAND3 * 2.0f - 1.0f);
    randomNormal = (dot(randomNormal, n) < 0.0f) ? -randomNormal : randomNormal;    // random normal on hemisphere, relative to the normal
    n = normalize(lerp(n, randomNormal, Roughness));
";
            }
        }

        protected string collisionResponseSource
        {
            get
            {
                var Source = "";
                if (roughSurface)
                    Source += roughSurfaceSource;
                Source += @"
    float projVelocity = dot(n, velocity);

    float3 normalVelocity = projVelocity * n;
    float3 tangentVelocity = velocity - normalVelocity;

    if (projVelocity < 0)
        velocity -= ((1 + Bounce) * projVelocity) * n;
    velocity -= Friction * tangentVelocity;

    age += (LifetimeLoss * lifetime);
";

                return Source;
            }
        }

        public class CollisionProperties
        {
            [Min(0), Tooltip("Sets how much bounce to apply after a collision.")]
            public float Bounce = 0.1f;
            [Min(0), Tooltip("Sets how much speed is lost after a collision.")]
            public float Friction = 0.0f;
            [Range(0, 1), Tooltip("Sets what proportion of a particle’s life is lost after a collision.")]
            public float LifetimeLoss = 0.0f;
        }

        public class RoughnessProperties
        {
            [Range(0, 1), Tooltip("Sets how much to randomly adjust the direction after a collision.")]
            public float Roughness = 0.0f;
        }

        public class RadiusProperties
        {
            [Tooltip("Sets the radius of the particle used for collision detection.")]
            public float radius = 0.1f;
        }
    }
}
