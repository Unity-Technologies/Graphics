using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute/position/Composition/{0}", variantProvider = typeof(PositionBaseProvider))]
    class PositionTorus : PositionBase
    {
        public override string name { get { return string.Format(base.name, "Arc Torus"); } }
        protected override float thicknessDimensions { get { return 2.0f; } }

        public class InputProperties
        {
            [Tooltip("Sets the torus used for positioning the particles.")]
            public TArcTorus arcTorus = TArcTorus.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("When using customized emission, control the position around the arc to emit particles from.")]
            public float arcSequencer = 0.0f;
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var allSlots = GetExpressionsFromSlots(this);
                foreach (var p in allSlots)
                    yield return p;

                var thickness = allSlots.FirstOrDefault(o => o.name == nameof(ThicknessProperties.Thickness)).exp;
                var majorRadius = allSlots.FirstOrDefault(o => o.name == "arcTorus_torus_majorRadius").exp;
                var minorRadius = allSlots.FirstOrDefault(o => o.name == "arcTorus_torus_minorRadius").exp;

                yield return new VFXNamedExpression(CalculateVolumeFactor(positionMode, majorRadius, thickness), "volumeFactor");

                var majorRadiusNotZero = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Greater, new VFXExpressionAbs(majorRadius), VFXOperatorUtility.EpsilonExpression[VFXValueType.Float]);
                var r = new VFXExpressionBranch(majorRadiusNotZero, VFXOperatorUtility.Saturate(minorRadius / majorRadius), VFXOperatorUtility.ZeroExpression[VFXValueType.Float]);
                yield return new VFXNamedExpression(r, "r"); // Saturate can be removed once degenerated torus are correctly handled

                var transformMatrix = allSlots.FirstOrDefault(o => o.name == "arcTorus_torus_transform").exp;
                var invFinalTransform = new VFXExpressionTransposeMatrix(new VFXExpressionInverseTRSMatrix(transformMatrix));
                yield return new VFXNamedExpression(invFinalTransform, "arcTorus_torus_inverseTranspose");
            }
        }

        protected override bool needDirectionWrite => true;

        public override string source
        {
            get
            {
                string outSource = @"";
                if (spawnMode == SpawnMode.Random)
                {
                    outSource += @"float3 u = RAND3;";
                    outSource += @"float arc = arcTorus_arc;";
                }
                else
                {
                    outSource += @"float3 u = float3(RAND, 1.0f, RAND);";
                    outSource += @"float arc = arcTorus_arc * arcSequencer;";
                }

                outSource += @"
float R = sqrt(volumeFactor + (1.0f - volumeFactor) * u.z);

float sinTheta,cosTheta;
sincos(u.x * UNITY_TWO_PI, sinTheta,cosTheta);

float2 s1_1 = R * r * float2(cosTheta, sinTheta) + float2(1, 0);
float2 s1_2 = R * r * float2(-cosTheta, sinTheta) + float2(1, 0);
float w = s1_1.x / (s1_1.x + s1_2.x);

float3 t;
float phi;
if (u.y < w)
{
    phi = arc * u.y / w;
    t = float3(s1_1.x, 0, s1_1.y);
}
else
{
    phi = arc * (u.y - w) / (1.0f - w);
    t = float3(s1_2.x, 0, s1_2.y);
}

float s, c;
sincos(phi, c, s);
float3 t2 = float3(c * t.x - s * t.y, c * t.y + s * t.x, t.z);

float3 finalPos = arcTorus_torus_majorRadius * t2;
float3 finalDir = t2;
finalPos = mul(arcTorus_torus_transform, float4(finalPos, 1.0f)).xyz;
finalDir = mul(arcTorus_torus_inverseTranspose, float4(finalDir, 0.0f)).xyz;
finalDir = normalize(finalDir);
";
                outSource += string.Format(composePositionFormatString, "finalPos");
                outSource += string.Format(composeDirectionFormatString, "finalDir");

                return outSource;
            }
        }
    }
}
