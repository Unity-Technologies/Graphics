using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.Operator
{
    class SampleMeshProvider : VariantProvider
    {
        protected virtual string nameTemplate { get; } = "Sample {0}";
        protected virtual Type operatorType { get; } = typeof(SampleMesh);

        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                string.Format(nameTemplate, "Mesh"),
                "Sampling",
                operatorType,
                new[] { new KeyValuePair<string, object>("source", SampleMesh.SourceType.Mesh) });

            yield return new Variant(
                string.Format(nameTemplate, "Skinned Mesh"),
                "Sampling",
                operatorType,
                new[] { new KeyValuePair<string, object>("source", SampleMesh.SourceType.SkinnedMeshRenderer) });
        }
    }

    [VFXHelpURL("Operator-SampleMesh")]
    [VFXInfo(variantProvider = typeof(SampleMeshProvider))]
    class SampleMesh : VFXOperator
    {
        public override string name
        {
            get
            {
                if (source == SourceType.Mesh)
                    return "Sample Mesh";
                else
                    return "Sample Skinned Mesh";
            }
        }

        public enum PlacementMode
        {
            Vertex,
            Edge,
            Surface
        };

        public class InputPropertiesMesh
        {
            [Tooltip("Specifies the Mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
        }

        public class InputPropertiesSkinnedMeshRenderer
        {
            [Tooltip("Specifies the Skinned Mesh Renderer component to sample from. The Skinned Mesh Renderer has to be an exposed entry.")]
            public SkinnedMeshRenderer skinnedMesh = null;
        }

        public class InputPropertiesPlacementVertex
        {
            [Tooltip("Sets the vertex index to read from.")]
            public uint vertex = 0u;
        }

        public enum SurfaceCoordinates
        {
            Barycentric,
            Uniform,
        }

        public class InputPropertiesEdge
        {
            [Tooltip("Sets the start index of the edge. The Block uses this index and the next index to render the line.")]
            public uint index = 0u;

            [Range(0, 1), Tooltip("Controls the percentage along the edge, from the start position to the end position, that the sample position is taken.")]
            public float edge;
        }

        public class InputPropertiesPlacementSurfaceBarycentricCoordinates
        {
            [Tooltip("The triangle index to read from.")]
            public uint triangle = 0u;

            [Tooltip("Barycentric coordinate (z is computed from x & y).")]
            public Vector2 barycentric;
        }

        public class InputPropertiesPlacementSurfaceLowDistorsionMapping
        {
            [Tooltip("The triangle index to read from.")]
            public uint triangle = 0u;

            [Tooltip("The uniform coordinate to sample the triangle at. The Block takes this value and maps it from a square coordinate to triangle space.")]
            public Vector2 square;
        }

        public class TransformProperties
        {
            [Tooltip("Sets the transform of the sampled Mesh. If used with a Skinned Mesh, it is applied after the transform of the root bone.")]
            public Transform transform = Transform.defaultValue;
        }

        [Flags]
        public enum VertexAttributeFlag
        {
            None = 0,
            Position = 1 << 0,
            Normal = 1 << 1,
            Tangent = 1 << 2,
            Bitangent = 1 << 3,
            BitangentSign = 1 << 4,
            Color = 1 << 5,
            TexCoord0 = 1 << 6,
            TexCoord1 = 1 << 7,
            TexCoord2 = 1 << 8,
            TexCoord3 = 1 << 9,
            TexCoord4 = 1 << 10,
            TexCoord5 = 1 << 11,
            TexCoord6 = 1 << 12,
            TexCoord7 = 1 << 13,
            BlendWeight = 1 << 14,
            BlendIndices = 1 << 15,

            Transform = 1 << 16,

            PreviousPosition = 1 << 17,
            PreviousNormal = 1 << 18,
            PreviousTangent = 1 << 19,
            PreviousBitangent = 1 << 20,
            Velocity = 1 << 21,

            PreviousTransform = 1 << 22
        }

        public enum SourceType
        {
            Mesh,
            SkinnedMeshRenderer
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Outputs the result of the Mesh sampling operation.")]
        private VertexAttributeFlag output = VertexAttributeFlag.Position | VertexAttributeFlag.Color | VertexAttributeFlag.TexCoord0;

        [VFXSetting, SerializeField, Tooltip("Specifies how Unity handles the sample when the custom vertex index is out the out of bounds of the vertex array.")]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Clamp;

        [VFXSetting, SerializeField, Tooltip("Specifies which primitive part of the mesh to sample from.")]
        private PlacementMode placementMode = PlacementMode.Vertex;

        [VFXSetting, SerializeField, Tooltip("Specifies how to sample the surface of a triangle.")]
        private SurfaceCoordinates surfaceCoordinates = SurfaceCoordinates.Uniform;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the kind of geometry to sample from.")]
        private SourceType source = SourceType.Mesh;

        public enum SkinnedRootTransform
        {
            None,
            ApplyLocalRootTransform,
            ApplyWorldRootTransform
        }

        [VFXSetting, SerializeField, Tooltip("Specifies the transform to apply to the root bone retrieved from the Skinned Mesh Renderer.")]
        public SkinnedRootTransform skinnedTransform = SkinnedRootTransform.ApplyLocalRootTransform;

        public override void Sanitize(int version)
        {
            if (version < 10)
            {
                SanitizeHelper.MigrateSampleMeshFrom9To10(this);
            }
            else
            {
                base.Sanitize(version);
            }
        }

        private static IEnumerable<VertexAttributeFlag> GetAttributesFromFlags(VertexAttributeFlag flags)
        {
            var vertexAttributes = Enum.GetValues(typeof(VertexAttributeFlag)).Cast<VertexAttributeFlag>();
            foreach (var vertexAttribute in vertexAttributes)
                if (vertexAttribute != VertexAttributeFlag.None && flags.HasFlag(vertexAttribute))
                    yield return vertexAttribute;
        }

        private IEnumerable<VertexAttributeFlag> GetOutputVertexAttributes()
        {
            return GetAttributesFromFlags(output);
        }

        public static readonly string kMixingSMRWorldAndLocalPostTransformMsg = @"Mixing World Root Bone transform with an input transform in Local space can yield unexpected results.
To avoid this, change the input Transform space from Local to World or None.";

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            var transformSlot = inputSlots.Last();
            if (actualSkinnedTransform == SkinnedRootTransform.ApplyWorldRootTransform && transformSlot.space == VFXSpace.Local)
            {
                report.RegisterError("MixingSMRWorldAndLocalPostTransformOperator", VFXErrorType.Warning, kMixingSMRWorldAndLocalPostTransformMsg, this);
            }

            var previousFlag = VertexAttributeFlag.PreviousNormal
                               | VertexAttributeFlag.PreviousTangent
                               | VertexAttributeFlag.PreviousBitangent
                               | VertexAttributeFlag.PreviousPosition
                               | VertexAttributeFlag.Velocity
                               | VertexAttributeFlag.PreviousTransform;

            if (source == SourceType.Mesh && (output & previousFlag) != 0)
            {
                report.RegisterError("PreviousOutputUsageOnMesh", VFXErrorType.Warning, "Sampling previous data is only available with SkinnedMeshRenderer sources.\nWhen using a Mesh source, previous outputs return the same values as current ones.", this);
            }
        }

        protected internal override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);

            //Called from VFXSlot.InvalidateExpressionTree, can be triggered from a space change, need to refresh block warning
            if (cause == InvalidationCause.kExpressionInvalidated)
            {
                model.RefreshErrors();
            }
        }

        private VFXSpace GetWantedOutputSpace()
        {
            //If we are applying the skinned mesh in world, using a local transform afterwards looks unexpected, forcing conversion of inputs to world.
            if (actualSkinnedTransform == SkinnedRootTransform.ApplyWorldRootTransform)
                return VFXSpace.World;

            //Otherwise, the input slot transform control the output space.
            var transformSlot = inputSlots.Last();
            if (!transformSlot.spaceable)
                throw new InvalidOperationException("Unexpected mesh slot: " + transformSlot);
            return transformSlot.space;
        }

        public override VFXSpace GetOutputSpaceFromSlot(VFXSlot outputSlot)
        {
            if (outputSlot.spaceable)
            {
                return GetWantedOutputSpace();
            }
            return VFXSpace.None;
        }

        private static string GetTooltip(VertexAttributeFlag attribute)
        {
            switch (attribute)
            {
                case VertexAttributeFlag.Position: return "Returns the position of the sampled vertex, edge, or surface coordinate.";
                case VertexAttributeFlag.Normal: return "Returns the normalized Normal vector of the sampled vertex, edge, or surface coordinate.";
                case VertexAttributeFlag.Tangent: return "Returns the non-normalized Tangent vector of the sampled vertex, edge, or surface coordinate.";
                case VertexAttributeFlag.Bitangent: return "Returns the Bitangent vector, which is a cross product of the Normal and Tangent vector multiplied by the Tangent sign.";
                case VertexAttributeFlag.Color: return "Returns the Vertex color. Sampling both Color and Color32 structures are supported.";
                case VertexAttributeFlag.TexCoord0:
                case VertexAttributeFlag.TexCoord1:
                case VertexAttributeFlag.TexCoord2:
                case VertexAttributeFlag.TexCoord3:
                case VertexAttributeFlag.TexCoord4:
                case VertexAttributeFlag.TexCoord5:
                case VertexAttributeFlag.TexCoord6:
                case VertexAttributeFlag.TexCoord7: return "Returns the vertex data stored in this TexCoord. TexCoord0 typically stores the UV data of the sampled mesh.";
                case VertexAttributeFlag.BlendWeight: return "Returns the bone blend weights for the mesh. This maps to the BLENDWEIGHTS semantic in the HLSL shading language.";
                case VertexAttributeFlag.BlendIndices: return "Returns the bone blend indices. This maps to the BLENDINDICES semantic in the HLSL shading language.";
                case VertexAttributeFlag.Transform: return "Returns the computed Transform after normalization and correction from the Normal, Tangent, Bitangent, and Position inputs.";
                case VertexAttributeFlag.PreviousPosition: return "Returns Previous Position considering previous root bone transform.";
                case VertexAttributeFlag.PreviousNormal: return "Returns Previous Normal considering previous root bone transform.";
                case VertexAttributeFlag.PreviousBitangent: return "Returns Previous Bitangent computed with cross(previousNormal, previousTangent.xyz) * previousTangent.w";
                case VertexAttributeFlag.Velocity: return "Returns Velocity vector, returns the difference between current and previous position divided by game time.";
                case VertexAttributeFlag.PreviousTransform: return "Returns the computed Transform after normalization and correction from the Previous Normal, Tangent, Bitangent, and Previous Position inputs.";
            }
            return string.Empty;
        }

        private static Type GetOutputType(VertexAttributeFlag attribute)
        {
            switch (attribute)
            {
                case VertexAttributeFlag.PreviousPosition:
                case VertexAttributeFlag.Position: return typeof(Position);
                case VertexAttributeFlag.PreviousNormal:
                case VertexAttributeFlag.Normal: return typeof(DirectionType);
                case VertexAttributeFlag.PreviousTangent:
                case VertexAttributeFlag.PreviousBitangent:
                case VertexAttributeFlag.Tangent:
                case VertexAttributeFlag.Bitangent:
                case VertexAttributeFlag.Velocity: return typeof(Vector);
                case VertexAttributeFlag.Color: return typeof(Vector4);
                case VertexAttributeFlag.TexCoord0:
                case VertexAttributeFlag.TexCoord1:
                case VertexAttributeFlag.TexCoord2:
                case VertexAttributeFlag.TexCoord3:
                case VertexAttributeFlag.TexCoord4:
                case VertexAttributeFlag.TexCoord5:
                case VertexAttributeFlag.TexCoord6:
                case VertexAttributeFlag.TexCoord7: return typeof(Vector4);
                case VertexAttributeFlag.BlendWeight: return typeof(Vector4);
                case VertexAttributeFlag.BlendIndices: return typeof(Vector4);
                case VertexAttributeFlag.BitangentSign: return typeof(float);
                case VertexAttributeFlag.Transform:
                case VertexAttributeFlag.PreviousTransform: return typeof(Transform);
                default: throw new InvalidOperationException("Unexpected attribute : " + attribute);
            }
        }

        private static VFXValueType GetSampledType(VertexAttribute attribute)
        {
            switch (attribute)
            {
                case VertexAttribute.Position: return VFXValueType.Float3;
                case VertexAttribute.Normal: return VFXValueType.Float3;
                case VertexAttribute.Tangent: return VFXValueType.Float4;
                case VertexAttribute.Color: return VFXValueType.Float4;
                case VertexAttribute.TexCoord0:
                case VertexAttribute.TexCoord1:
                case VertexAttribute.TexCoord2:
                case VertexAttribute.TexCoord3:
                case VertexAttribute.TexCoord4:
                case VertexAttribute.TexCoord5:
                case VertexAttribute.TexCoord6:
                case VertexAttribute.TexCoord7: return VFXValueType.Float4;
                case VertexAttribute.BlendWeight:
                case VertexAttribute.BlendIndices: return VFXValueType.Float4;
                default: throw new InvalidOperationException("Unexpected attribute: " + attribute);
            }
        }

        private static VertexAttribute GetActualVertexAttribute(VertexAttributeFlag attribute)
        {
            switch (attribute)
            {
                case VertexAttributeFlag.Position: return VertexAttribute.Position;
                case VertexAttributeFlag.Normal: return VertexAttribute.Normal;
                case VertexAttributeFlag.BitangentSign:
                case VertexAttributeFlag.Tangent: return VertexAttribute.Tangent;
                case VertexAttributeFlag.Color: return VertexAttribute.Color;
                case VertexAttributeFlag.TexCoord0: return VertexAttribute.TexCoord0;
                case VertexAttributeFlag.TexCoord1: return VertexAttribute.TexCoord1;
                case VertexAttributeFlag.TexCoord2: return VertexAttribute.TexCoord2;
                case VertexAttributeFlag.TexCoord3: return VertexAttribute.TexCoord3;
                case VertexAttributeFlag.TexCoord4: return VertexAttribute.TexCoord4;
                case VertexAttributeFlag.TexCoord5: return VertexAttribute.TexCoord5;
                case VertexAttributeFlag.TexCoord6: return VertexAttribute.TexCoord6;
                case VertexAttributeFlag.TexCoord7: return VertexAttribute.TexCoord7;
                case VertexAttributeFlag.BlendWeight: return VertexAttribute.BlendWeight;
                case VertexAttributeFlag.BlendIndices: return VertexAttribute.BlendIndices;
                default: throw new InvalidOperationException("Unexpected attribute : " + attribute);
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var settings in base.filteredOutSettings)
                    yield return settings;

                if (placementMode != PlacementMode.Surface)
                    yield return nameof(surfaceCoordinates);

                if (source != SourceType.SkinnedMeshRenderer)
                    yield return nameof(skinnedTransform);
            }
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var props = base.inputProperties;
                if (source == SourceType.Mesh)
                    props = props.Concat(PropertiesFromType(nameof(InputPropertiesMesh)));
                else if (source == SourceType.SkinnedMeshRenderer)
                    props = props.Concat(PropertiesFromType(nameof(InputPropertiesSkinnedMeshRenderer)));
                else
                    throw new InvalidOperationException("Unexpected source type : " + source);

                if (placementMode == PlacementMode.Vertex)
                    props = props.Concat(PropertiesFromType(nameof(InputPropertiesPlacementVertex)));
                else if (placementMode == PlacementMode.Surface)
                {
                    if (surfaceCoordinates == SurfaceCoordinates.Barycentric)
                        props = props.Concat(PropertiesFromType(nameof(InputPropertiesPlacementSurfaceBarycentricCoordinates)));
                    else if (surfaceCoordinates == SurfaceCoordinates.Uniform)
                        props = props.Concat(PropertiesFromType(nameof(InputPropertiesPlacementSurfaceLowDistorsionMapping)));
                    else
                        throw new InvalidOperationException("Unexpected surfaceCoordinates : " + surfaceCoordinates);
                }
                else if (placementMode == PlacementMode.Edge)
                    props = props.Concat(PropertiesFromType(nameof(InputPropertiesEdge)));
                else
                    throw new InvalidOperationException("Unexpected placementMode : " + placementMode);

                props = props.Concat(PropertiesFromType(nameof(TransformProperties)));
                return props;
            }
        }

        private SkinnedRootTransform actualSkinnedTransform
        {
            get
            {
                if (source == SourceType.SkinnedMeshRenderer)
                    return skinnedTransform;
                return SkinnedRootTransform.None;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                foreach (var vertexAttribute in GetOutputVertexAttributes())
                {
                    var outputType = GetOutputType(vertexAttribute);
                    var tooltip = GetTooltip(vertexAttribute);
                    yield return new VFXPropertyWithValue(
                        new VFXProperty(outputType,
                            ObjectNames.NicifyVariableName(vertexAttribute.ToString()),
                            new TooltipAttribute(tooltip)));
                }
            }
        }

        private static VFXExpression SampleVertexAttribute(VFXExpression source, VFXExpression vertexIndex, VertexAttribute vertexAttribute, VFXSkinnedMeshFrame frame = VFXSkinnedMeshFrame.Current)
        {
            bool skinnedMesh = source.valueType == VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);

            var channelIndex = VFXValue.Constant<uint>((uint)vertexAttribute);
            var meshVertexStride = new VFXExpressionMeshVertexStride(mesh, channelIndex);
            var meshChannelOffset = new VFXExpressionMeshChannelOffset(mesh, channelIndex);

            var outputType = GetSampledType(vertexAttribute);
            VFXExpression sampled = null;

            var meshChannelFormatAndDimension = new VFXExpressionMeshChannelInfos(mesh, channelIndex);
            var vertexOffset = vertexIndex * meshVertexStride + meshChannelOffset;

            if (!skinnedMesh)
            {
                if (vertexAttribute == VertexAttribute.Color)
                    sampled = new VFXExpressionSampleMeshColor(source, vertexOffset, meshChannelFormatAndDimension);
                else if (outputType == VFXValueType.Float)
                    sampled = new VFXExpressionSampleMeshFloat(source, vertexOffset, meshChannelFormatAndDimension);
                else if (outputType == VFXValueType.Float2)
                    sampled = new VFXExpressionSampleMeshFloat2(source, vertexOffset, meshChannelFormatAndDimension);
                else if (outputType == VFXValueType.Float3)
                    sampled = new VFXExpressionSampleMeshFloat3(source, vertexOffset, meshChannelFormatAndDimension);
                else if (outputType == VFXValueType.Float4)
                    sampled = new VFXExpressionSampleMeshFloat4(source, vertexOffset, meshChannelFormatAndDimension);
            }
            else
            {
                if (frame == VFXSkinnedMeshFrame.Previous && outputType != VFXValueType.Float3 && outputType != VFXValueType.Float4)
                    throw new InvalidOperationException("Unexpected type to sample previous frame : " + outputType);

                if (vertexAttribute == VertexAttribute.Color)
                    sampled = new VFXExpressionSampleSkinnedMeshRendererColor(source, vertexOffset, meshChannelFormatAndDimension);
                else if (outputType == VFXValueType.Float)
                    sampled = new VFXExpressionSampleSkinnedMeshRendererFloat(source, vertexOffset, meshChannelFormatAndDimension);
                else if (outputType == VFXValueType.Float2)
                    sampled = new VFXExpressionSampleSkinnedMeshRendererFloat2(source, vertexOffset, meshChannelFormatAndDimension);
                else if (outputType == VFXValueType.Float3)
                    sampled = new VFXExpressionSampleSkinnedMeshRendererFloat3(source, vertexOffset, meshChannelFormatAndDimension, frame);
                else if (outputType == VFXValueType.Float4)
                    sampled = new VFXExpressionSampleSkinnedMeshRendererFloat4(source, vertexOffset, meshChannelFormatAndDimension, frame);
            }

            if (sampled == null)
                throw new InvalidOperationException("Unexpected Mesh Sampling type.");

            return sampled;
        }

        private static SpaceableType GetSpaceableFromVertexAttribute(VertexAttributeFlag currentAttribute)
        {
            if ((currentAttribute & (currentAttribute - 1)) != 0)
                throw new InvalidOperationException("Unexpected not single bit current attribute: " + currentAttribute);

            switch (currentAttribute)
            {
                case VertexAttributeFlag.Position:
                case VertexAttributeFlag.PreviousPosition:
                    return SpaceableType.Position;
                case VertexAttributeFlag.Normal:
                case VertexAttributeFlag.Tangent:
                case VertexAttributeFlag.Bitangent:
                case VertexAttributeFlag.PreviousNormal:
                case VertexAttributeFlag.PreviousTangent:
                case VertexAttributeFlag.PreviousBitangent:
                    return SpaceableType.Vector;
                case VertexAttributeFlag.PreviousTransform:
                case VertexAttributeFlag.Transform:
                    return SpaceableType.Matrix;
            }

            return SpaceableType.None;
        }

        private static bool ShouldUsePreviousMatrix(VertexAttributeFlag flag)
        {
            switch (flag)
            {
                case VertexAttributeFlag.PreviousNormal:
                case VertexAttributeFlag.PreviousTangent:
                case VertexAttributeFlag.PreviousBitangent:
                case VertexAttributeFlag.PreviousPosition:
                case VertexAttributeFlag.PreviousTransform:
                    return true;
            }

            return false;
        }

        public struct VFXMeshTransform
        {
            public VFXExpression current;
            public VFXExpression previous;
        }

        private static VFXExpression ComputeVertexAttribute(IEnumerable<VFXExpression> sampledVertexAttribute, VertexAttributeFlag currentAttribute, VFXMeshTransform postTransform)
        {
            if (postTransform.current == null || postTransform.previous == null)
                throw new InvalidOperationException("Unexpected null transform");

            if ((currentAttribute & (currentAttribute - 1)) != 0)
                throw new InvalidOperationException("Unexpected not single bit current attribute: " + currentAttribute);

            //Compute expected attribute
            VFXExpression sampled;
            if (currentAttribute == VertexAttributeFlag.Tangent || currentAttribute == VertexAttributeFlag.PreviousTangent)
            {
                sampled = sampledVertexAttribute.First().xyz;
            }
            else if (currentAttribute == VertexAttributeFlag.BitangentSign)
            {
                sampled = sampledVertexAttribute.First().w;
            }
            else if (currentAttribute == VertexAttributeFlag.Bitangent || currentAttribute == VertexAttributeFlag.PreviousBitangent)
            {
                var sampledNormal = sampledVertexAttribute.First();
                var sampledTangent = sampledVertexAttribute.Last();
                if (sampledNormal == sampledTangent)
                    throw new InvalidOperationException("Unexpected tangent/normal equality");
                sampled = VFXOperatorUtility.Cross(sampledNormal, sampledTangent.xyz) * sampledTangent.www;
            }
            else if (currentAttribute == VertexAttributeFlag.Normal || currentAttribute == VertexAttributeFlag.PreviousNormal)
            {
                //N.B.: Only normal is actually normalized
                var sampledNormal = sampledVertexAttribute.First();
                sampled = VFXOperatorUtility.Normalize(sampledNormal);
            }
            else if (currentAttribute == VertexAttributeFlag.Transform || currentAttribute == VertexAttributeFlag.PreviousTransform)
            {
                var position = sampledVertexAttribute.ElementAt(0);
                var normal = sampledVertexAttribute.ElementAt(1);
                var tangent = sampledVertexAttribute.ElementAt(2);

                //insure normal/tangent aren't zero
                var sqrLengthNormal = VFXOperatorUtility.Dot(normal, normal);
                var sqrLengthTangent = VFXOperatorUtility.Dot(tangent, tangent);

                var srqEpsilon = VFXOperatorUtility.EpsilonSqrExpression[VFXValueType.Float];
                var nullNormal = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Less, sqrLengthNormal, srqEpsilon);
                var nullTangent = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Less, sqrLengthTangent, srqEpsilon);

                normal = new VFXExpressionBranch(nullNormal, VFXValue.Constant(Vector3.up), normal);
                tangent = new VFXExpressionBranch(nullTangent, VFXValue.Constant(Vector3.forward), tangent.xyz);

                //compute initial basis
                normal = VFXOperatorUtility.Normalize(normal);
                var bitangent = VFXOperatorUtility.Cross(normal, tangent);
                bitangent = VFXOperatorUtility.Normalize(bitangent);

                //insure tangent orthonormal with normal (cross of normalized input, not need to renormalize)
                tangent = VFXOperatorUtility.Cross(bitangent, normal);
                sampled = new VFXExpressionAxisToMatrix(bitangent, normal, tangent, position);
            }
            else if (currentAttribute == VertexAttributeFlag.Velocity)
            {
                var currentPosition = sampledVertexAttribute.ElementAt(0);
                var previousPosition = sampledVertexAttribute.ElementAt(1);

                currentPosition = TransformExpression(currentPosition, SpaceableType.Position, postTransform.current);
                previousPosition = TransformExpression(previousPosition, SpaceableType.Position, postTransform.previous);

                //Warning: This is not VFX deltaTime here, the skin mesh renderer isn't using the vfx time step.
                var deltaTime = VFXBuiltInExpression.GameDeltaTime;
                deltaTime = new VFXExpressionMax(deltaTime, VFXOperatorUtility.EpsilonExpression[VFXValueType.Float]);
                deltaTime = VFXOperatorUtility.CastFloat(deltaTime, VFXValueType.Float3);
                sampled = (currentPosition - previousPosition) / deltaTime;

                //Cancel following transform which has already been done
                postTransform.current = postTransform.previous = null;
            }
            else
            {
                //Default: 1:1 between flag & actual vertex attribute
                sampled = sampledVertexAttribute.First();
            }

            if (postTransform.current != null && postTransform.previous != null)
            {
                var previous = ShouldUsePreviousMatrix(currentAttribute);
                var spaceableType = GetSpaceableFromVertexAttribute(currentAttribute);
                sampled = TransformExpression(sampled, spaceableType, previous ? postTransform.previous : postTransform.current);
            }

            return sampled;
        }

        private static IEnumerable<VFXExpression> SampleNeededVertexAttribute(VFXExpression source, VFXExpression vertexIndex, VertexAttributeFlag currentAttribute)
        {
            if ((currentAttribute & (currentAttribute - 1)) != 0)
                throw new InvalidOperationException("Unexpected not single bit current attribute: " + currentAttribute);

            if (currentAttribute == VertexAttributeFlag.Tangent || currentAttribute == VertexAttributeFlag.BitangentSign)
            {
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Tangent);
            }
            else if (currentAttribute == VertexAttributeFlag.Bitangent)
            {
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Normal);
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Tangent);
            }
            else if (currentAttribute == VertexAttributeFlag.PreviousBitangent)
            {
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Normal, VFXSkinnedMeshFrame.Previous);
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Tangent, VFXSkinnedMeshFrame.Previous);
            }
            else if (currentAttribute == VertexAttributeFlag.Transform)
            {
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Position);
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Normal);
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Tangent);
            }
            else if (currentAttribute == VertexAttributeFlag.PreviousTransform)
            {
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Position, VFXSkinnedMeshFrame.Previous);
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Normal, VFXSkinnedMeshFrame.Previous);
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Tangent, VFXSkinnedMeshFrame.Previous);
            }
            else if (currentAttribute == VertexAttributeFlag.PreviousNormal)
            {
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Normal, VFXSkinnedMeshFrame.Previous);
            }
            else if (currentAttribute == VertexAttributeFlag.PreviousTangent)
            {
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Tangent, VFXSkinnedMeshFrame.Previous);
            }
            else if (currentAttribute == VertexAttributeFlag.PreviousPosition)
            {
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Position, VFXSkinnedMeshFrame.Previous);
            }
            else if (currentAttribute == VertexAttributeFlag.Velocity)
            {
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Position, VFXSkinnedMeshFrame.Current);
                yield return SampleVertexAttribute(source, vertexIndex, VertexAttribute.Position, VFXSkinnedMeshFrame.Previous);
            }
            else
            {
                //Default: 1:1 between flag & actual vertex attribute
                var vertexAttribute = GetActualVertexAttribute(currentAttribute);
                yield return SampleVertexAttribute(source, vertexIndex, vertexAttribute);
            }
        }

        public static IEnumerable<VFXExpression> SampleVertexAttribute(VFXExpression source, VFXExpression vertexIndex, IEnumerable<VertexAttributeFlag> vertexAttributes, VFXMeshTransform postTransform)
        {
            foreach (var currentAttribute in vertexAttributes)
            {
                var neededAttribute = SampleNeededVertexAttribute(source, vertexIndex, currentAttribute);
                var computedAttribute = ComputeVertexAttribute(neededAttribute, currentAttribute, postTransform);
                yield return computedAttribute;
            }
        }

        private static IEnumerable<VFXExpression> SampleVertexAttribute(VFXExpression source, VFXExpression vertexIndex, VFXOperatorUtility.SequentialAddressingMode mode, IEnumerable<VertexAttributeFlag> vertexAttributes, VFXMeshTransform postTransform)
        {
            bool skinnedMesh = source.valueType == VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);
            var meshVertexCount = new VFXExpressionMeshVertexCount(mesh);
            vertexIndex = VFXOperatorUtility.ApplyAddressingMode(vertexIndex, meshVertexCount, mode);
            return SampleVertexAttribute(source, vertexIndex, vertexAttributes, postTransform);
        }

        public static IEnumerable<VFXExpression> SampleEdgeAttribute(VFXExpression source, VFXExpression index, VFXExpression lerp, IEnumerable<VertexAttributeFlag> vertexAttributes, VFXMeshTransform postTransform)
        {
            bool skinnedMesh = source.valueType == VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);

            var meshIndexFormat = new VFXExpressionMeshIndexFormat(mesh);

            var oneUint = VFXOperatorUtility.OneExpression[VFXValueType.Uint32];
            var threeUint = VFXOperatorUtility.ThreeExpression[VFXValueType.Uint32];

            var nextIndex = index + oneUint;

            //Loop triangle
            var loop = VFXOperatorUtility.Modulo(nextIndex, threeUint);
            var predicat = new VFXExpressionCondition(VFXValueType.Uint32, VFXCondition.NotEqual, loop, VFXOperatorUtility.ZeroExpression[VFXValueType.Uint32]);
            nextIndex = new VFXExpressionBranch(predicat, nextIndex, nextIndex - threeUint);

            var sampledIndex_A = new VFXExpressionSampleIndex(mesh, index, meshIndexFormat);
            var sampledIndex_B = new VFXExpressionSampleIndex(mesh, nextIndex, meshIndexFormat);

            foreach (var attribute in vertexAttributes)
            {
                var neededAttribute_A = SampleNeededVertexAttribute(source, sampledIndex_A, attribute);
                var neededAttribute_B = SampleNeededVertexAttribute(source, sampledIndex_B, attribute);

                var interpolatedAttribute = Enumerable.Zip(neededAttribute_A, neededAttribute_B, (a, b) =>
                {
                    var outputValueType = a.valueType;
                    var s = VFXOperatorUtility.CastFloat(lerp, outputValueType);
                    var r = VFXOperatorUtility.Lerp(a, b, s);
                    return r;
                });

                yield return ComputeVertexAttribute(interpolatedAttribute, attribute, postTransform);
            }
        }

        private static IEnumerable<VFXExpression> SampleEdgeAttribute(VFXExpression source, VFXExpression index, VFXExpression x, VFXOperatorUtility.SequentialAddressingMode mode, IEnumerable<VertexAttributeFlag> vertexAttributes, VFXMeshTransform postTransform)
        {
            bool skinnedMesh = source.valueType == VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);
            var meshIndexCount = new VFXExpressionMeshIndexCount(mesh);

            index = VFXOperatorUtility.ApplyAddressingMode(index, meshIndexCount, mode);
            return SampleEdgeAttribute(source, index, x, vertexAttributes, postTransform);
        }

        static IEnumerable<T> Zip3<T>(IEnumerable<T> first, IEnumerable<T> second, IEnumerable<T> third, Func<T, T, T, T> func)
        {
            using (var itFirst = first.GetEnumerator())
            using (var itSecond = second.GetEnumerator())
            using (var itThird = third.GetEnumerator())
            {
                while (itFirst.MoveNext() && itSecond.MoveNext() && itThird.MoveNext())
                    yield return func(itFirst.Current, itSecond.Current, itThird.Current);
            }
        }

        public static IEnumerable<VFXExpression> SampleTriangleAttribute(VFXExpression source, VFXExpression triangleIndex, VFXExpression coord, SurfaceCoordinates coordMode, IEnumerable<VertexAttributeFlag> vertexAttributes, VFXMeshTransform postTranform)
        {
            bool skinnedMesh = source.valueType == VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);

            var meshIndexFormat = new VFXExpressionMeshIndexFormat(mesh);

            var threeUint = VFXOperatorUtility.ThreeExpression[VFXValueType.Uint32];
            var baseIndex = triangleIndex * threeUint;

            var sampledIndex_A = new VFXExpressionSampleIndex(mesh, baseIndex, meshIndexFormat);
            var sampledIndex_B = new VFXExpressionSampleIndex(mesh, baseIndex + VFXValue.Constant<uint>(1u), meshIndexFormat);
            var sampledIndex_C = new VFXExpressionSampleIndex(mesh, baseIndex + VFXValue.Constant<uint>(2u), meshIndexFormat);

            var allInputValues = new List<VFXExpression>();
            VFXExpression barycentricCoordinates = null;
            var one = VFXOperatorUtility.OneExpression[VFXValueType.Float];
            if (coordMode == SurfaceCoordinates.Barycentric)
            {
                var barycentricCoordinateInput = coord;
                barycentricCoordinates = new VFXExpressionCombine(barycentricCoordinateInput.x, barycentricCoordinateInput.y, one - barycentricCoordinateInput.x - barycentricCoordinateInput.y);
            }
            else if (coordMode == SurfaceCoordinates.Uniform)
            {
                //https://hal.archives-ouvertes.fr/hal-02073696v2/document
                var input = coord;

                var half2 = VFXOperatorUtility.HalfExpression[VFXValueType.Float2];
                var zero = VFXOperatorUtility.ZeroExpression[VFXValueType.Float];
                var t = input * half2;
                var offset = t.y - t.x;
                var pred = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Greater, offset, zero);
                var t2 = new VFXExpressionBranch(pred, t.y + offset, t.y);
                var t1 = new VFXExpressionBranch(pred, t.x, t.x - offset);
                var t3 = one - t2 - t1;
                barycentricCoordinates = new VFXExpressionCombine(t1, t2, t3);

                /* Possible variant See http://inis.jinr.ru/sl/vol1/CMC/Graphics_Gems_1,ed_A.Glassner.pdf (p24) uniform distribution from two numbers in triangle generating barycentric coordinate
                var input = VFXOperatorUtility.Saturate(inputExpression[2]);
                var s = input.x;
                var t = VFXOperatorUtility.Sqrt(input.y);
                var a = one - t;
                var b = (one - s) * t;
                var c = s * t;
                barycentricCoordinates = new VFXExpressionCombine(a, b, c);
                */
            }
            else
            {
                throw new InvalidOperationException("No supported surfaceCoordinates : " + coord);
            }

            foreach (var attribute in vertexAttributes)
            {
                var neededAttribute_A = SampleNeededVertexAttribute(source, sampledIndex_A, attribute);
                var neededAttribute_B = SampleNeededVertexAttribute(source, sampledIndex_B, attribute);
                var neededAttribute_C = SampleNeededVertexAttribute(source, sampledIndex_C, attribute);

                var interpolatedAttribute = Zip3(neededAttribute_A, neededAttribute_B, neededAttribute_C, (a, b, c) =>
                {
                    var outputValueType = a.valueType;

                    var barycentricCoordinateX = VFXOperatorUtility.CastFloat(barycentricCoordinates.x, outputValueType);
                    var barycentricCoordinateY = VFXOperatorUtility.CastFloat(barycentricCoordinates.y, outputValueType);
                    var barycentricCoordinateZ = VFXOperatorUtility.CastFloat(barycentricCoordinates.z, outputValueType);

                    var r = a * barycentricCoordinateX + b * barycentricCoordinateY + c * barycentricCoordinateZ;
                    return r;
                });

                yield return ComputeVertexAttribute(interpolatedAttribute, attribute, postTranform);
            }
        }

        private static IEnumerable<VFXExpression> SampleTriangleAttribute(VFXExpression source, VFXExpression triangleIndex, VFXExpression coord, VFXOperatorUtility.SequentialAddressingMode mode, SurfaceCoordinates coordMode, IEnumerable<VertexAttributeFlag> vertexAttributes, VFXMeshTransform postTranform)
        {
            bool skinnedMesh = source.valueType == VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);
            var UintThree = VFXOperatorUtility.ThreeExpression[VFXValueType.Uint32];

            var meshIndexCount = new VFXExpressionMeshIndexCount(mesh);
            var triangleCount = meshIndexCount / UintThree;
            triangleIndex = VFXOperatorUtility.ApplyAddressingMode(triangleIndex, triangleCount, mode);

            return SampleTriangleAttribute(source, triangleIndex, coord, coordMode, vertexAttributes, postTranform);
        }

        public static VFXMeshTransform ComputeTransformMatrix(VFXExpression source, SkinnedRootTransform smrTransform, VFXExpression postTransform)
        {
            var transfom = new VFXMeshTransform()
            {
                current = postTransform,
                previous = postTransform
            };

            if (smrTransform != SkinnedRootTransform.None)
            {
                VFXExpression transformRootBoneCurrent;
                VFXExpression transformRootBonePrevious;
                if (source.valueType != VFXValueType.SkinnedMeshRenderer)
                    throw new InvalidOperationException();

                if (smrTransform == SkinnedRootTransform.ApplyLocalRootTransform)
                {
                    transformRootBoneCurrent = new VFXExpressionRootBoneTransformFromSkinnedMeshRenderer(source, VFXSkinnedTransform.LocalRootBoneTransform, VFXSkinnedMeshFrame.Current);
                    transformRootBonePrevious = new VFXExpressionRootBoneTransformFromSkinnedMeshRenderer(source, VFXSkinnedTransform.LocalRootBoneTransform, VFXSkinnedMeshFrame.Previous);
                }
                else if (smrTransform == SkinnedRootTransform.ApplyWorldRootTransform)
                {
                    transformRootBoneCurrent = new VFXExpressionRootBoneTransformFromSkinnedMeshRenderer(source, VFXSkinnedTransform.WorldRootBoneTransform, VFXSkinnedMeshFrame.Current);
                    transformRootBonePrevious = new VFXExpressionRootBoneTransformFromSkinnedMeshRenderer(source, VFXSkinnedTransform.WorldRootBoneTransform, VFXSkinnedMeshFrame.Previous);
                }
                else
                {
                    throw new InvalidOperationException("Unexpected SMR Transform" + smrTransform);
                }

                transfom.current = new VFXExpressionTransformMatrix(postTransform, transformRootBoneCurrent);
                transfom.previous = new VFXExpressionTransformMatrix(postTransform, transformRootBonePrevious);
            }

            return transfom;
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var source = inputExpression[0];
            var matrix = ComputeTransformMatrix(source, actualSkinnedTransform, inputExpression.Last());

            VFXExpression[] outputExpressions = null;
            if (placementMode == PlacementMode.Vertex)
            {
                var sampled = SampleVertexAttribute(inputExpression[0], inputExpression[1], mode, GetOutputVertexAttributes(), matrix);
                outputExpressions = sampled.ToArray();
            }
            else if (placementMode == PlacementMode.Edge)
            {
                var sampled = SampleEdgeAttribute(inputExpression[0], inputExpression[1], inputExpression[2], mode, GetOutputVertexAttributes(), matrix);
                outputExpressions = sampled.ToArray();
            }
            else if (placementMode == PlacementMode.Surface)
            {
                var sampled = SampleTriangleAttribute(inputExpression[0], inputExpression[1], inputExpression[2], mode, surfaceCoordinates, GetOutputVertexAttributes(), matrix);
                outputExpressions = sampled.ToArray();
            }
            else
            {
                throw new InvalidOperationException("Not supported placement mode " + placementMode);
            }
            return outputExpressions;
        }
    }
}
