using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXHelpURL("Block-ConformToSphere")]
    [VFXInfo(name = "Attractor Shape|Sphere", category = "Force")]
    class ConformToSphere : VFXBlock
    {
        public override string name => "Attractor Shape Sphere";
        public override VFXContextType compatibleContexts { get { return VFXContextType.Update; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

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
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            [Tooltip("Sets the sphere to which particles can conform.")]
            public Sphere Sphere = Sphere.defaultValue;
            [Tooltip("Sets the speed with which particles are attracted towards the surface of the sphere.")]
            public float attractionSpeed = 5.0f;
            [Tooltip("Sets the strength of the force pulling particles towards the sphere.")]
            public float attractionForce = 20.0f;
            [Tooltip("Sets the distance at which particles attempt to stick to the sphere.")]
            public float stickDistance = 0.1f;
            [Tooltip("Sets the strength of the force keeping particles on the sphere.")]
            public float stickForce = 50.0f;
        }

        public override string source
        {
            get
            {
                return @"
float3 dir = Sphere_center - position;
float distToCenter = length(dir);
float distToSurface = distToCenter - Sphere_radius;
dir /= max(VFX_FLT_MIN,distToCenter); // safe normalize
float spdNormal = dot(dir,velocity);
float ratio = smoothstep(0.0,stickDistance * 2.0,abs(distToSurface));
float tgtSpeed = sign(distToSurface) * attractionSpeed * ratio;
float deltaSpeed = tgtSpeed - spdNormal;
velocity += sign(deltaSpeed) * min(abs(deltaSpeed),deltaTime * lerp(stickForce,attractionForce,ratio)) * dir / mass;";
            }
        }
    }
}
