using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.BlockLibrary
{
    abstract class CollisionBase : VFXBlock
    {
        public enum Mode
        {
            Solid,
            Inverted
        }

        [VFXSetting, Tooltip("The Collider can be either a solid volume, or an empty volume, with an infinite filled volume surrounding it.")]
        public Mode mode = Mode.Solid;
        [VFXSetting, Tooltip("Enable random bending of the collision normal to simulate collision with a rough surface.")]
        public bool roughSurface = false;

        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
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
                yield return new VFXAttributeInfo(new VFXAttribute("mass", VFXValue.Constant(1.0f)), VFXAttributeMode.Read);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this))
                    yield return p;

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");

                if (mode == Mode.Solid)
                    yield return new VFXNamedExpression(VFXValue.Constant(1.0f), "colliderSign");
                else
                    yield return new VFXNamedExpression(VFXValue.Constant(-1.0f), "colliderSign");
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
    n = normalize(lerp(n, randomNormal, Chaos));
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
    projVelocity *= mass;

    float3 normalVelocity = projVelocity * n;
    float3 tangentVelocity = velocity - normalVelocity;

    if (projVelocity < 0)
        velocity -= ((1 + Elasticity) * projVelocity) * n;
    velocity -= Friction * tangentVelocity;

    age += (LifetimeLoss * lifetime);
";

                return Source;
            }
        }

        public class CollisionProperties
        {
            [Min(0), Tooltip("How much bounce to apply after a collision.")]
            public float Elasticity = 0.1f;
            [Min(0), Tooltip("How much speed is lost after a collision.")]
            public float Friction = 0.0f;
            [Range(0, 1), Tooltip("The proportion of a particle's life that is lost after a collision.")]
            public float LifetimeLoss = 0.0f;
        }

        public class RoughnessProperties
        {
            [Range(0, 1), Tooltip("How much to randomly adjust the normal after a collision.")]
            public float Chaos = 0.0f;
        }
    }
}
