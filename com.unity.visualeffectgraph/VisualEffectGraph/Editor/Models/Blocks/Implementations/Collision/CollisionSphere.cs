using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.VFX.BlockLibrary
{
    [VFXInfo(category = "Collision")]
    class CollisionSphere : VFXBlock
    {
        public enum Mode
        {
            Solid,
            Hollow
        }
        [VFXSetting]
        public Mode mode = Mode.Solid;

        public override string name { get { return "Collide Sphere"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
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
                    yield return new VFXNamedExpression(VFXValue.Constant(1.0f), "sign");
                else
                    yield return new VFXNamedExpression(VFXValue.Constant(-1.0f), "sign");
            }
        }
        public class InputProperties
        {
            public Sphere Sphere = new Sphere() { radius = 1.0f };
            public float Elasticity = 0.1f;
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 dir = Sphere_center - nextPos;
float sqrLength = dot(dir,dir);
if ( sign * sqrLength <= sign * Sphere_radius * Sphere_radius)
{
    float dist = sqrt(sqrLength);
    float3 n = sign * dir / dist;
    float projVelocity = dot(n,velocity);

    if (projVelocity > 0)
        velocity -= ((1 + Elasticity) * projVelocity) * n;

    position += sign * n * (dist - Sphere_radius);
}";
                return Source;
            }
        }
    }
}
