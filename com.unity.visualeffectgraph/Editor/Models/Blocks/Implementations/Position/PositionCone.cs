using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionCone : PositionBase
    {
        public enum HeightMode
        {
            Base,
            Volume
        }

        [VFXSetting, Tooltip("Controls whether particles are spawned on the base of the cone, or throughout the entire volume.")]
        public HeightMode heightMode;

        public override string name { get { return "Position (Cone)"; } }
        protected override float thicknessDimensions { get { return 2.0f; } }

        public class InputProperties
        {
            [Tooltip("Sets the cone used for positioning the particles.")]
            public ArcCone ArcCone = ArcCone.defaultValue;
        }

        public class InputPropertiesBis
        {
            [Tooltip("Sets the cone used for positioning the particles.")]
            public ArcConeBis ArcCone = ArcConeBis.defaultValue;
        }

        public class InputPropertiesTer
        {
            [Tooltip("Sets the cone used for positioning the particles.")]
            public ArcConeTer ArcCone = ArcConeTer.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("Sets the position along the height to emit particles from when ‘Custom Emission’ is used.")]
            public float HeightSequencer = 0.0f;
            [Range(0, 1), Tooltip("Sets the position on the arc to emit particles from when ‘Custom Emission’ is used.")]
            public float ArcSequencer = 0.0f;
        }

        public enum Type_Of_Transform_For_ArcCone
        {
            TwoVectorsDirection,
            AxisUpAndRotation,
            EulerAngle
        }

        [VFXSetting]
        public Type_Of_Transform_For_ArcCone arcCone_ModeTest;

        /* Remove this*/
        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = Enumerable.Empty<VFXPropertyWithValue>();
                if (arcCone_ModeTest == Type_Of_Transform_For_ArcCone.TwoVectorsDirection)
                {
                    properties = PropertiesFromType(GetInputPropertiesTypeName());
                }
                else if (arcCone_ModeTest == Type_Of_Transform_For_ArcCone.AxisUpAndRotation)
                {
                    properties = PropertiesFromType("InputPropertiesBis");
                }
                else if (arcCone_ModeTest == Type_Of_Transform_For_ArcCone.EulerAngle)
                {
                    properties = PropertiesFromType("InputPropertiesTer");
                }

                if (supportsVolumeSpawning)
                {
                    if (positionMode == PositionMode.ThicknessAbsolute || positionMode == PositionMode.ThicknessRelative)
                        properties = properties.Concat(PropertiesFromType("ThicknessProperties"));
                }

                if (spawnMode == SpawnMode.Custom)
                    properties = properties.Concat(PropertiesFromType("CustomProperties"));

                return properties;
            }
        }

        static void ApplyRotationAroundYAxis(ref VFXExpression i, ref VFXExpression j, ref VFXExpression k, VFXExpression rotation)
        {
            var cosTheta = new VFXExpressionCos(rotation);
            var sinTheta = new VFXExpressionSin(rotation);
            var one = VFXOperatorUtility.OneExpression[VFXValueType.Float];
            var minusOne = VFXOperatorUtility.MinusOneExpression[VFXValueType.Float];
            var zero = VFXOperatorUtility.ZeroExpression[VFXValueType.Float];

            var matrix = new VFXExpressionVector4sToMatrix(
                new VFXExpressionCombine(cosTheta,              zero,       sinTheta,   zero),
                new VFXExpressionCombine(zero,                  one,        zero,       zero),
                new VFXExpressionCombine(sinTheta * minusOne,   zero,       cosTheta,   zero),
                new VFXExpressionCombine(zero,                  zero,       zero,       one)
            );

            //Not sure it's ideal for constant folding
            i = new VFXExpressionTransformDirection(matrix, i);
            j = new VFXExpressionTransformDirection(matrix, j);
            k = new VFXExpressionTransformDirection(matrix, k);
        }

        static VFXExpression RemoveTranslatePart(VFXExpression matrix)
        {
            var i = new VFXExpressionMatrixToVector3s(matrix, VFXValue.Constant(0));
            var j = new VFXExpressionMatrixToVector3s(matrix, VFXValue.Constant(1));
            var k = new VFXExpressionMatrixToVector3s(matrix, VFXValue.Constant(2));
            var o = VFXValue.Constant(new Vector4(0, 0, 0, 1));
            return new VFXExpressionVector4sToMatrix(i, j, k, o);
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this).Where(      e => e.name != "Thickness"
                                                                        ||  e.name != "ArcCone_center"))
                    yield return p; //TODOPAUL, exclude unused slot

                yield return new VFXNamedExpression(CalculateVolumeFactor(positionMode, 0, 1), "volumeFactor");

                VFXExpression radius0 = inputSlots[0][3].GetExpression();
                VFXExpression radius1 = inputSlots[0][4].GetExpression();
                VFXExpression height = inputSlots[0][5].GetExpression();
                VFXExpression tanSlope = (radius1 - radius0) / height;
                VFXExpression slope = new VFXExpressionATan(tanSlope);
                yield return new VFXNamedExpression(new VFXExpressionCombine(new VFXExpression[] { new VFXExpressionSin(slope), new VFXExpressionCos(slope) }), "sincosSlope");

                var slotSpace = inputSlots[0].space;
                var systemSpace = ((VFXDataParticle)GetData()).space;

                VFXExpression center = inputSlots[0][0].GetExpression();
                if (arcCone_ModeTest == Type_Of_Transform_For_ArcCone.EulerAngle)
                {
                    VFXExpression eulerAngle = inputSlots[0][1].GetExpression();

                    var zeroF3 = VFXOperatorUtility.ZeroExpression[VFXValueType.Float3];
                    var oneF3 = VFXOperatorUtility.OneExpression[VFXValueType.Float3];

                    VFXExpression rotationMatrix = new VFXExpressionTRSToMatrix(zeroF3, eulerAngle, oneF3);
                    /*VFXExpression axisInverter = VFXValue.Constant(new Matrix4x4(   new Vector4(1, 0, 0, 0),
                                                                                    new Vector4(0, 0, 1, 0),
                                                                                    new Vector4(0, 1, 0, 0),
                                                                                    new Vector4(0, 0, 0, 1)));*/
                    VFXExpression axisInverter = VFXValue.Constant(new Matrix4x4(   new Vector4(1, 0, 0, 0),
                                                                                    new Vector4(0, 1, 0, 0),
                                                                                    new Vector4(0, 0, 1, 0),
                                                                                    new Vector4(0, 0, 0, 1)));
                    rotationMatrix = new VFXExpressionTransformMatrix(rotationMatrix, axisInverter);

                    if (slotSpace != systemSpace)
                    {
                        if (systemSpace == VFXCoordinateSpace.World)
                            rotationMatrix = new VFXExpressionTransformMatrix(rotationMatrix, RemoveTranslatePart(VFXBuiltInExpression.LocalToWorld));
                        else if (systemSpace == VFXCoordinateSpace.Local)
                            rotationMatrix = new VFXExpressionTransformMatrix(rotationMatrix, RemoveTranslatePart(VFXBuiltInExpression.WorldToLocal));
                        else
                            throw new System.NotImplementedException();
                    }

                    //Can be simplified using matrix composition.
                    var translateMatrix = new VFXExpressionTRSToMatrix(center, zeroF3, oneF3);
                    var matrix = new VFXExpressionTransformMatrix(translateMatrix, rotationMatrix);
                    yield return new VFXNamedExpression(matrix, "transformMatrix");
                }
                else
                {
                    VFXExpression upAxis = inputSlots[0][1].GetExpression();
                    upAxis = VFXOperatorUtility.Normalize(upAxis);
                    VFXExpression leftAxis;
                    if (arcCone_ModeTest == Type_Of_Transform_For_ArcCone.AxisUpAndRotation)
                    {
                        leftAxis = VFXValue.Constant(new Vector3(1, 0, 0)); //Local space or is depending of space of slot ? TODO

                        var upAxisRotation = inputSlots[0][2].GetExpression();
                        var cosTheta = new VFXExpressionCos(upAxisRotation);
                        var sinTheta = new VFXExpressionSin(upAxisRotation);
                        var zero = VFXOperatorUtility.ZeroExpression[VFXValueType.Float];
                        leftAxis = new VFXExpressionCombine(sinTheta, zero, cosTheta);

                        if (systemSpace != slotSpace)
                        {
                            if (systemSpace == VFXCoordinateSpace.World)
                                leftAxis = new VFXExpressionTransformDirection(VFXBuiltInExpression.LocalToWorld, leftAxis);
                            else if (systemSpace == VFXCoordinateSpace.Local)
                                leftAxis = new VFXExpressionTransformDirection(VFXBuiltInExpression.WorldToLocal, leftAxis);
                            else
                                throw new System.NotImplementedException();
                        }
                    }
                    else
                    {
                        leftAxis = inputSlots[0][2].GetExpression();
                        leftAxis = VFXOperatorUtility.Normalize(leftAxis);
                    }

                    var directionAxis = VFXOperatorUtility.Cross(upAxis, leftAxis);
                    //TODOPAUL : check length of directionAxis
                    directionAxis = VFXOperatorUtility.Normalize(directionAxis);
                    leftAxis = VFXOperatorUtility.Cross(directionAxis, upAxis);

                    //TODOPAUL : Check if we need this cast
                    directionAxis = VFXOperatorUtility.CastFloat(directionAxis, VFXValueType.Float4, 0.0f);
                    upAxis = VFXOperatorUtility.CastFloat(upAxis, VFXValueType.Float4, 0.0f);
                    leftAxis = VFXOperatorUtility.CastFloat(leftAxis, VFXValueType.Float4, 0.0f);
                    center = VFXOperatorUtility.CastFloat(center, VFXValueType.Float4, 1.0f);

                    VFXExpression matrix = new VFXExpressionVector4sToMatrix(directionAxis, upAxis, leftAxis, center);
                    yield return new VFXNamedExpression(matrix, "transformMatrix");

                    //TODOPAUL : Could reduce number of cbuffer, don't think it's relevant.
                    //yield return new VFXNamedExpression(directionAxis, "transformMatrix_a");
                    //yield return new VFXNamedExpression(upAxis, "transformMatrix_b");
                    //yield return new VFXNamedExpression(leftAxis, "transformMatrix_c");
                    //yield return new VFXNamedExpression(center, "transformMatrix_d");
                }

            }
        }

        protected override bool needDirectionWrite
        {
            get
            {
                return true;
            }
        }

        public override string source
        {
            get
            {
                string outSource = "";

                if (spawnMode == SpawnMode.Random)
                    outSource += @"float theta = ArcCone_arc * RAND;";
                else
                    outSource += @"float theta = ArcCone_arc * ArcSequencer;";

                outSource += @"
float rNorm = sqrt(volumeFactor + (1 - volumeFactor) * RAND);

float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
float2 pos = (sincosTheta * rNorm);
";

                if (heightMode == HeightMode.Base)
                {
                    outSource += @"
float hNorm = 0.0f;
";
                }
                else if (spawnMode == SpawnMode.Random)
                {
                    float distributionExponent = positionMode == PositionMode.Surface ? 2.0f : 3.0f;
                    outSource += $@"
float hNorm = 0.0f;
if (abs(ArcCone_radius0 - ArcCone_radius1) > VFX_EPSILON)
{{
    // Uniform distribution on cone
    float heightFactor = ArcCone_radius0 / max(VFX_EPSILON,ArcCone_radius1);
    float heightFactorPow = pow(heightFactor, {distributionExponent});
    hNorm = pow(heightFactorPow + (1.0f - heightFactorPow) * RAND, rcp({distributionExponent}));
    hNorm = (hNorm - heightFactor) / (1.0f - heightFactor); // remap on [0,1]
}}
else
    hNorm = RAND; // Uniform distribution on cylinder
";
                }
                else
                {
                    outSource += @"
float hNorm = HeightSequencer;
";
                }

                outSource += @"
direction.xzy = normalize(float3(pos * sincosSlope.x, sincosSlope.y));
float3 finalPos = lerp(float3(pos * ArcCone_radius0, 0.0f), float3(pos * ArcCone_radius1, ArcCone_height), hNorm);
";
                //Maybe revert this xzy for all cases, could be always embedded in transformMatrix
                if (arcCone_ModeTest != Type_Of_Transform_For_ArcCone.EulerAngle)
                    outSource += "finalPos = finalPos.xzy;";

                outSource += @"
finalPos = mul(transformMatrix, float4(finalPos, 1.0f)).xyz;
position += finalPos;
";
                return outSource;
            }
        }
    }
}
