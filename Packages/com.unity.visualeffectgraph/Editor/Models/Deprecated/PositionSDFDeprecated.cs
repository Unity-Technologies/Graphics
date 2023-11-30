using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class PositionSDFDeprecated : PositionBase
    {
        public override string name => "Set Position (Signed Distance Field)";

        public class InputProperties
        {
            [Tooltip("Sets the Signed Distance Field to sample from.")]
            public Texture3D SDF = VFXResources.defaultResources.signedDistanceField;
            [Tooltip("Sets the transform with which to position, scale, or rotate the field.")]
            public OrientedBox FieldTransform = OrientedBox.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("When using customized emission, control the position around the arc to emit particles from.")]
            public float ArcSequencer = 0.0f;
        }

        protected override bool needDirectionWrite { get { return true; } }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies whether we want to kill particles whose position is off the desired surface or volume")]
        public bool killOutliers = false;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies the number of steps used by the block to project the particle on the surface of the SDF. This can impact performance, but can yield less outliers. "), Min(1u)]
        public uint projectionSteps = 2u;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                foreach (var attrib in base.attributes)
                {
                    yield return attrib;
                }
                if (killOutliers)
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);
            }
        }

        // Not used anymore so remove implementation due to LinQ usage
        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string source
        {
            get
            {
                string outSource = @"float cosPhi = 2.0f * RAND - 1.0f;";
                if (spawnMode == SpawnMode.Random)
                    outSource += @"float theta = TWO_PI * RAND;";
                else
                    outSource += @"float theta = TWO_PI * ArcSequencer;";
                switch (positionMode)
                {
                    case (PositionMode.Surface):
                        outSource += @" float Thickness = 0.0f;";
                        break;
                    case (PositionMode.Volume):
                        outSource += @" float Thickness = scalingFactor;";
                        break;
                    case (PositionMode.ThicknessRelative):
                        outSource += @" Thickness *= scalingFactor * 0.5f;";
                        break;
                }
                outSource += @"
//Initialize position within texture bounds
float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
sincosTheta *= sqrt(1.0f - cosPhi * cosPhi);
direction = float3(sincosTheta, cosPhi) *  sqrt(3)/2;
float maxDir = max(0.5f, max(abs(direction.x), max(abs(direction.y), abs(direction.z))));
direction = direction * (projectionRayWithMargin/maxDir);
float3 tPos = direction * pow(RAND, 1.0f/3.0f);
float3 coord = tPos + 0.5f;
float3 wPos, n, worldNormal;

for(uint proj_step=0; proj_step < n_steps; proj_step++){

    float dist = SampleSDF(SDF, coord);
    n = -normalize(SampleSDFDerivativesFast(SDF, coord, dist));
    dist *= scalingFactor;

    //Projection on surface/volume
    float3 delta;
    worldNormal = VFXSafeNormalize(mul(float4(n, 0), InvFieldTransform).xyz);
    if (dist > 0)
        delta = dist * worldNormal;
    else
    {
        delta = min(dist + Thickness, 0) * worldNormal;
    }

    wPos =  mul(FieldTransform, float4(tPos,1)).xyz + delta;
    tPos = mul(InvFieldTransform, float4(wPos,1)).xyz;
    coord = tPos + 0.5f;

}
position = wPos;
direction = -worldNormal;
                        ";
                if (killOutliers)
                {
                    outSource += @"

 float dist = SampleSDF(SDF, coord);
 if (dist * scalingFactor > 0.01)
    alive = false;
";
                }

                return outSource;
            }
        }

        public override void Sanitize(int version)
        {
            var newPositionShape = ScriptableObject.CreateInstance<PositionShape>();
            SanitizeHelper.MigrateBlockPositionToComposed(GetGraph(), GetParent().position, newPositionShape, this, PositionShapeBase.Type.SignedDistanceField);
            ReplaceModel(newPositionShape, this);
        }
    }
}
