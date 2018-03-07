using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class Turbulence : VFXBlock
    {
        public class InputProperties
        {
            [Tooltip("The position, rotation and scale of the turbulence field")]
            public Transform FieldTransform = Transform.defaultValue;

            [Tooltip("Number of Octaves of the noise (Max 10)")]
            public int NumOctaves = 3;

            [Range(0.0f, 1.0f), Tooltip("The roughness of the turbulence")]
            public float Roughness = 0.5f;

            [Tooltip("Intensity of the motion vectors")]
            public float Intensity;
            [Tooltip("The drag coefficient used to drive particles")]
            public float DragCoefficient;
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Texture Data Layout : Actual Values, or Unsigned Normalized (Centered on Gray)")]
        public TextureDataEncoding DataEncoding = TextureDataEncoding.Signed;

        public override string name { get { return "Turbulence"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var input in GetExpressionsFromSlots(this))
                {
                    if (input.name == "FieldTransform")
                        yield return new VFXNamedExpression(new VFXExpressionInverseMatrix(input.exp), "InvFieldTransform");
                    yield return input;
                }

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public override string source
        {
            get
            {
                return @"
float3 vectorFieldCoord = mul(InvFieldTransform, float4(position,1.0f)).xyz;

float3 value = Noise3D(vectorFieldCoord + 0.5f, min(NumOctaves,10), Roughness);
value = mul(FieldTransform,float4(value,0.0f)).xyz * Intensity;
float3 relativeForce = value - velocity;
velocity += relativeForce * min(1.0,(DragCoefficient * deltaTime) / mass);
";
            }
        }
    }
}
