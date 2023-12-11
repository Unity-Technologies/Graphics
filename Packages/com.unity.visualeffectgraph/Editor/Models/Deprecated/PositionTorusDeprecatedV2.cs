using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class PositionTorusDeprecatedV2 : PositionBase
    {
        public override void Sanitize(int version)
        {
            var newPositionShape = ScriptableObject.CreateInstance<PositionShape>();
            SanitizeHelper.MigrateBlockPositionToComposed(GetGraph(), GetParent().position, newPositionShape, this, PositionShapeBase.Type.Torus);
            ReplaceModel(newPositionShape, this);
        }

        public override string name { get { return string.Format(base.name, "Arc Torus"); } }

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
