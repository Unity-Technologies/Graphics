using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    abstract class CollisionBase : VFXBlock
    {
        public static string GetNamePrefix(Behavior b)
        {
            switch(b)
            {
                case Behavior.None:         return "Trigger";
                case Behavior.Collision:    return "Collision";
                case Behavior.Kill:         return "Kill";

                default: throw new NotImplementedException();
            }
        }

        public enum Behavior
        {
            None = 0,
            Collision = 1 << 0,
            Kill = 1 << 1,
        }

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

        public enum CollisionAttributesMode
        {
            NoWrite,
            WritePunctalContactOnly,
            WriteAlways,
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies the behavior upon collision. Either collision response, kill particle or None. If None is set, the block can be used as a trigger for collision events.")]
        public Behavior behavior = Behavior.Collision;
        [VFXSetting, Tooltip("Specifies the collision shape mode. The collider can either be a solid volume which particles cannot enter, or an empty volume which particles cannot leave.")]
        public Mode mode = Mode.Solid;
        [VFXSetting, Tooltip("Specifies the collision radius of each particle. This can be set to none (zero), automatically inherited from the particle size, or a custom value.")]
        public RadiusMode radiusMode = RadiusMode.None;
        [VFXSetting, Tooltip("Specifies if the block should write collision attributes (HasCollisionEvent, CollisionEventNormal, CollisionEventPosition and CollisionEventCount). Attributes can be written in case of punctual collisions only or always.")]
        public CollisionAttributesMode collisionAttributes;
        [VFXSetting, Tooltip("When enabled, randomness is added to the direction in which particles bounce back to simulate collision with a rough surface.")]
        public bool roughSurface = false;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, the rough normal is written to collisionEventNormal. The geometric normal (without the roughness perturbation) is written otherwise")]
        public bool writeRoughNormal = true;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, bounce speed threshold can be overridden (default is 1) to allow finer control over the stability/convergence of collisions and punctual contact detection.")]
        public bool overrideBounceThreshold = false;

        public override VFXContextType compatibleContexts
        {
            get
            {
                if (behavior.HasFlag(Behavior.Collision))
                    return VFXContextType.InitAndUpdate;
                return VFXContextType.InitAndUpdateAndOutput;
            }
        }

        public override VFXDataType compatibleData => VFXDataType.Particle;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.OldVelocity, VFXAttributeMode.Read);

                if (behavior.HasFlag(Behavior.Collision))
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                    yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                    yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);

                    // TODO This should be conditional
                    yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.ReadWrite);
                    yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);

                    // No need for size attributes here, they are explicitly used in collision parameters
                }
                else
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);

                    if (behavior.HasFlag(Behavior.Kill))
                    {
                        yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);
                    }
                }

                if (roughSurface)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                }

               	if (collisionAttributes != CollisionAttributesMode.NoWrite)
               	{
               	    yield return new VFXAttributeInfo(VFXAttribute.HasCollisionEvent, VFXAttributeMode.Write); //collision detected at instant T then reset
                    yield return new VFXAttributeInfo(VFXAttribute.CollisionEventNormal, VFXAttributeMode.Write);
                    yield return new VFXAttributeInfo(VFXAttribute.CollisionEventPosition, VFXAttributeMode.Write);
                    yield return new VFXAttributeInfo(VFXAttribute.CollisionEventCount, VFXAttributeMode.ReadWrite);
               	}
            }
        }

        protected virtual bool allowInvertedCollision => true;

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

                if (behavior == Behavior.Collision && !overrideBounceThreshold)
                    yield return new VFXNamedExpression(VFXValue.Constant(1.0f), nameof(CollisionProperties.BounceSpeedThreshold));
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this))
                    yield return p;

                foreach (var p in collisionParameters)
                    yield return p;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (!allowInvertedCollision)
                    yield return nameof(mode);

                if (!behavior.HasFlag(Behavior.Collision))
                    yield return nameof(roughSurface);

                if (!roughSurface || collisionAttributes == CollisionAttributesMode.NoWrite)
                    yield return nameof(writeRoughNormal);

                if (behavior != Behavior.Collision)
                    yield return nameof(overrideBounceThreshold);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = PropertiesFromType(GetInputPropertiesTypeName());

                if (behavior.HasFlag(Behavior.Collision))
                {
                    properties = properties.Concat(PropertiesFromType(nameof(CollisionProperties)));
                    if (!overrideBounceThreshold)
                        properties = properties.Where(p => p.property.name != nameof(CollisionProperties.BounceSpeedThreshold));
                }

                if (radiusMode == RadiusMode.Custom)
                {
                    properties = properties.Concat(PropertiesFromType(nameof(RadiusProperties)));
                }

                if (roughSurface)
                {
                    properties = properties.Concat(PropertiesFromType(nameof(RoughnessProperties)));
                }

                return properties;
            }
        }

        protected virtual string collisionDetection { get; }

        public sealed override string source
        {
            get
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append(@"bool hit = false;
float tHit = 0.0f; // t normalized
float3 hitNormal = (float3)0.0f;
float3 hitPos = (float3)0.0f;
");

                stringBuilder.Append(collisionDetection);


                stringBuilder.Append(@"
if (hit)
{
    // Heuristic to categorize punctual vs continuous contact
    bool isPunctualContact = dot(hitNormal, oldVelocity) < -VFX_EPSILON;
    float3 geometricNormal = hitNormal;");

                if (roughSurface)
                {
                    stringBuilder.Append(@"

    if (isPunctualContact)
    {
        float3 randomNormal = normalize(RAND3 * 2.0f - 1.0f);
        randomNormal = (dot(randomNormal, hitNormal) < 0.0f) ? -randomNormal : randomNormal; // random normal on hemisphere, relative to the normal
        hitNormal = normalize(lerp(geometricNormal, randomNormal, Roughness));
    }");
                }

                if (behavior.HasFlag(Behavior.Collision))
                {
                    stringBuilder.Append(@"

    float projVel = dot(hitNormal, velocity);

    float3 normalVel = projVel * hitNormal;
    float3 tangentVel = velocity - normalVel;

    if (projVel < 0)
    {
        // For continuous contact, we cancel the velocity normal to the surface
	    float restitutionCoef = isPunctualContact ? Bounce : 0.0f;
        if (projVel > -BounceSpeedThreshold)
        {
            float bounceAttenuation = -projVel / (BounceSpeedThreshold + VFX_EPSILON);
            restitutionCoef *= bounceAttenuation;
            isPunctualContact = false; // Don't send punctual collision event under velocity threshold
        }
	    velocity -= (1.0f + restitutionCoef) * normalVel; // Reflect the normal component
    }
    else
        isPunctualContact = false;

    velocity -= (1 - exp(-(Friction * deltaTime) / mass)) * tangentVel; // Friction on the tangential component

    age += (LifetimeLoss * lifetime); // lifetime loss
    position = hitPos - (deltaTime * tHit) * velocity; // Backtrack particle");
                }
                else if (behavior.HasFlag(Behavior.Kill))
                {
                    stringBuilder.Append(@"
    alive = false;");
                }

                if (collisionAttributes != CollisionAttributesMode.NoWrite)
                {
                    stringBuilder.Append($@"

    // Collision event
    if ({(collisionAttributes == CollisionAttributesMode.WritePunctalContactOnly && behavior == Behavior.Collision ? "isPunctualContact" : "true")})
    {{
        hasCollisionEvent = true;
        collisionEventNormal = {(writeRoughNormal ? "hitNormal" : "geometricNormal")};
        collisionEventPosition = hitPos - geometricNormal * radius;
        collisionEventCount += 1;
    }}");
                }

                stringBuilder.Append(@"
}
");

                return stringBuilder.ToString();
            }
        }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            if (behavior == Behavior.None && collisionAttributes == CollisionAttributesMode.NoWrite)
            {
                report.RegisterError("NoEffectColliderBlock", VFXErrorType.Warning, "The block behavior is set to None and collision attributes are not written. This block does not have any effect.", this);
            }

            if (behavior != Behavior.Collision && collisionAttributes == CollisionAttributesMode.WritePunctalContactOnly)
            {
                report.RegisterError("NotCollisionAndWriteOnPunctual", VFXErrorType.Warning, "Punctual contacts work only with Collision behavior. The collision attributes mode should be changed to Write Always.", this);
            }

            if (behavior == Behavior.Collision)
            {
                // Check if another node is writing velocity afterwards
                var parent = GetParent();
                if (parent != null)
                {
                    int index = parent.GetIndex(this);
                    int blockCount = parent.GetNbChildren();
                    bool velocityWriteFound = false;

                    for (int i = index + 1; i < blockCount; ++i)
                    {
                        var block = parent[i];
                        if (block.enabled && block is not CollisionBase)
                        {
                            foreach (var attrib in block.attributes)
                                if (attrib.mode.HasFlag(VFXAttributeMode.Write) && attrib.attrib.Equals(VFXAttribute.Velocity))
                                {
                                    velocityWriteFound = true;
                                    break;
                                }
                        }

                        if (velocityWriteFound)
                            break;
                    }

                    if (velocityWriteFound)
                    {
                        report.RegisterError("VelocityWrittenAfterCollision", VFXErrorType.Warning, "Velocity attribute is written after this block in the context. You should only write velocity prior to collisions otherwise they might not work as expected", this);
                    }
                }
            }
        }

        public class CollisionProperties
        {
            [Min(0), Tooltip("Sets how much bounce to apply after a collision. 1 is fully elastic collision. 0 is no bounce at all. Values higher than 1 causes the system to gain energy.")]
            public float Bounce = 0.1f;
            [Min(0), Tooltip("Sets the fiction applied by the surface. The higher the friction, the more speed particles lose in contact with the surface.")]
            public float Friction = 0.0f;
            [VFXSetting, Min(0.0f), Tooltip("Sets the minimum speed at which full bounce is restituted. Speeds below this threshold receives a linear attenuation on their bounce and are not considered as punctual. Tweak this value to make sure particles converge towards a stable state and no micro bounces are taken into account for punctual collision events. Default is 1.")]
            public float BounceSpeedThreshold = 1.0f;
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
