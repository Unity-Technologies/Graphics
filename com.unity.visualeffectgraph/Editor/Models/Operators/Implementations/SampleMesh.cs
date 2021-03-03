using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.Operator
{
    class SampleMeshProvider : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "source", Enum.GetValues(typeof(SampleMesh.SourceType)).Cast<object>().ToArray() },
                };
            }
        }
    }

    [VFXInfo(category = "Sampling", variantProvider = typeof(SampleMeshProvider), experimental = true)]
    class SampleMesh : VFXOperator
    {
        override public string name
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

        [Flags]
        public enum VertexAttributeFlag
        {
            None = 0,
            Position = 1 << 0,
            Normal = 1 << 1,
            Tangent = 1 << 2,
            Color = 1 << 3,
            TexCoord0 = 1 << 4,
            TexCoord1 = 1 << 5,
            TexCoord2 = 1 << 6,
            TexCoord3 = 1 << 7,
            TexCoord4 = 1 << 8,
            TexCoord5 = 1 << 9,
            TexCoord6 = 1 << 10,
            TexCoord7 = 1 << 11,
            BlendWeight = 1 << 12,
            BlendIndices = 1 << 13
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

        private bool HasOutput(VertexAttributeFlag flag)
        {
            return (output & flag) == flag;
        }

        private IEnumerable<VertexAttribute> GetOutputVertexAttributes()
        {
            var vertexAttributes = Enum.GetValues(typeof(VertexAttributeFlag)).Cast<VertexAttributeFlag>();
            foreach (var vertexAttribute in vertexAttributes)
                if (vertexAttribute != VertexAttributeFlag.None && HasOutput(vertexAttribute))
                    yield return GetActualVertexAttribute(vertexAttribute);
        }

        private static Type GetOutputType(VertexAttribute attribute)
        {
            switch (attribute)
            {
                case VertexAttribute.Position: return typeof(Vector3);
                case VertexAttribute.Normal: return typeof(Vector3);
                case VertexAttribute.Tangent: return typeof(Vector4);
                case VertexAttribute.Color: return typeof(Vector4);
                case VertexAttribute.TexCoord0:
                case VertexAttribute.TexCoord1:
                case VertexAttribute.TexCoord2:
                case VertexAttribute.TexCoord3:
                case VertexAttribute.TexCoord4:
                case VertexAttribute.TexCoord5:
                case VertexAttribute.TexCoord6:
                case VertexAttribute.TexCoord7: return typeof(Vector4);
                case VertexAttribute.BlendWeight: return typeof(Vector4);
                case VertexAttribute.BlendIndices: return typeof(Vector4);
                default: throw new InvalidOperationException("Unexpected attribute : " + attribute);
            }
        }

        private static VertexAttribute GetActualVertexAttribute(VertexAttributeFlag attribute)
        {
            switch (attribute)
            {
                case VertexAttributeFlag.Position: return VertexAttribute.Position;
                case VertexAttributeFlag.Normal: return VertexAttribute.Normal;
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

                return props;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                foreach (var vertexAttribute in GetOutputVertexAttributes())
                {
                    var outputType = GetOutputType(vertexAttribute);
                    yield return new VFXPropertyWithValue(new VFXProperty(outputType, vertexAttribute.ToString()));
                }
            }
        }

        public static IEnumerable<VFXExpression> SampleVertexAttribute(VFXExpression source, VFXExpression vertexIndex, IEnumerable<VertexAttribute> vertexAttributes)
        {
            bool skinnedMesh = source.valueType == UnityEngine.VFX.VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);

            foreach (var vertexAttribute in vertexAttributes)
            {
                var channelIndex = VFXValue.Constant<uint>((uint)vertexAttribute);
                var meshVertexStride = new VFXExpressionMeshVertexStride(mesh, channelIndex);
                var meshChannelOffset = new VFXExpressionMeshChannelOffset(mesh, channelIndex);

                var outputType = GetOutputType(vertexAttribute);
                VFXExpression sampled = null;

                var meshChannelFormatAndDimension = new VFXExpressionMeshChannelInfos(mesh, channelIndex);
                var vertexOffset = vertexIndex * meshVertexStride + meshChannelOffset;

                if (!skinnedMesh)
                {
                    if (vertexAttribute == VertexAttribute.Color)
                        sampled = new VFXExpressionSampleMeshColor(source, vertexOffset, meshChannelFormatAndDimension);
                    else if (outputType == typeof(float))
                        sampled = new VFXExpressionSampleMeshFloat(source, vertexOffset, meshChannelFormatAndDimension);
                    else if (outputType == typeof(Vector2))
                        sampled = new VFXExpressionSampleMeshFloat2(source, vertexOffset, meshChannelFormatAndDimension);
                    else if (outputType == typeof(Vector3))
                        sampled = new VFXExpressionSampleMeshFloat3(source, vertexOffset, meshChannelFormatAndDimension);
                    else
                        sampled = new VFXExpressionSampleMeshFloat4(source, vertexOffset, meshChannelFormatAndDimension);
                }
                else
                {
                    if (vertexAttribute == VertexAttribute.Color)
                        sampled = new VFXExpressionSampleSkinnedMeshRendererColor(source, vertexOffset, meshChannelFormatAndDimension);
                    else if (outputType == typeof(float))
                        sampled = new VFXExpressionSampleSkinnedMeshRendererFloat(source, vertexOffset, meshChannelFormatAndDimension);
                    else if (outputType == typeof(Vector2))
                        sampled = new VFXExpressionSampleSkinnedMeshRendererFloat2(source, vertexOffset, meshChannelFormatAndDimension);
                    else if (outputType == typeof(Vector3))
                        sampled = new VFXExpressionSampleSkinnedMeshRendererFloat3(source, vertexOffset, meshChannelFormatAndDimension);
                    else
                        sampled = new VFXExpressionSampleSkinnedMeshRendererFloat4(source, vertexOffset, meshChannelFormatAndDimension);
                }

                yield return sampled;
            }
        }

        public static IEnumerable<VFXExpression> SampleVertexAttribute(VFXExpression source, VFXExpression vertexIndex, VFXOperatorUtility.SequentialAddressingMode mode, IEnumerable<VertexAttribute> vertexAttributes)
        {
            bool skinnedMesh = source.valueType == UnityEngine.VFX.VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);
            var meshVertexCount = new VFXExpressionMeshVertexCount(mesh);
            vertexIndex = VFXOperatorUtility.ApplyAddressingMode(vertexIndex, meshVertexCount, mode);
            return SampleVertexAttribute(source, vertexIndex, vertexAttributes);
        }

        public static IEnumerable<VFXExpression> SampleEdgeAttribute(VFXExpression source, VFXExpression index, VFXExpression lerp, IEnumerable<VertexAttribute> vertexAttributes)
        {
            bool skinnedMesh = source.valueType == UnityEngine.VFX.VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);

            var meshIndexFormat = new VFXExpressionMeshIndexFormat(mesh);

            var oneInt = VFXOperatorUtility.OneExpression[UnityEngine.VFX.VFXValueType.Int32];
            var oneUint = VFXOperatorUtility.OneExpression[UnityEngine.VFX.VFXValueType.Uint32];
            var threeUint = VFXOperatorUtility.ThreeExpression[UnityEngine.VFX.VFXValueType.Uint32];

            var nextIndex = index + oneUint;

            //Loop triangle
            var loop = VFXOperatorUtility.Modulo(nextIndex, threeUint);
            var predicat = new VFXExpressionCondition(UnityEngine.VFX.VFXValueType.Uint32, VFXCondition.NotEqual, loop, VFXOperatorUtility.ZeroExpression[UnityEngine.VFX.VFXValueType.Uint32]);
            nextIndex = new VFXExpressionBranch(predicat, nextIndex, nextIndex - threeUint);

            var sampledIndex_A = new VFXExpressionSampleIndex(mesh, index, meshIndexFormat);
            var sampledIndex_B = new VFXExpressionSampleIndex(mesh, nextIndex, meshIndexFormat);

            var sampling_A = SampleVertexAttribute(source, sampledIndex_A, vertexAttributes).ToArray();
            var sampling_B = SampleVertexAttribute(source, sampledIndex_B, vertexAttributes).ToArray();

            for (int i = 0; i < vertexAttributes.Count(); ++i)
            {
                var outputValueType = sampling_A[i].valueType;
                var s = VFXOperatorUtility.CastFloat(lerp, outputValueType);
                yield return VFXOperatorUtility.Lerp(sampling_A[i], sampling_B[i], s);
            }
        }

        public static IEnumerable<VFXExpression> SampleEdgeAttribute(VFXExpression source, VFXExpression index, VFXExpression x, VFXOperatorUtility.SequentialAddressingMode mode, IEnumerable<VertexAttribute> vertexAttributes)
        {
            bool skinnedMesh = source.valueType == UnityEngine.VFX.VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);
            var meshIndexCount = new VFXExpressionMeshIndexCount(mesh);

            index = VFXOperatorUtility.ApplyAddressingMode(index, meshIndexCount, mode);
            return SampleEdgeAttribute(source, index, x, vertexAttributes);
        }

        public static IEnumerable<VFXExpression> SampleTriangleAttribute(VFXExpression source, VFXExpression triangleIndex, VFXExpression coord, SurfaceCoordinates coordMode, IEnumerable<VertexAttribute> vertexAttributes)
        {
            bool skinnedMesh = source.valueType == UnityEngine.VFX.VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);

            var meshIndexCount = new VFXExpressionMeshIndexCount(mesh);
            var meshIndexFormat = new VFXExpressionMeshIndexFormat(mesh);

            var threeUint = VFXOperatorUtility.ThreeExpression[UnityEngine.VFX.VFXValueType.Uint32];
            var baseIndex = triangleIndex * threeUint;

            var sampledIndex_A = new VFXExpressionSampleIndex(mesh, baseIndex, meshIndexFormat);
            var sampledIndex_B = new VFXExpressionSampleIndex(mesh, baseIndex + VFXValue.Constant<uint>(1u), meshIndexFormat);
            var sampledIndex_C = new VFXExpressionSampleIndex(mesh, baseIndex + VFXValue.Constant<uint>(2u), meshIndexFormat);

            var allInputValues = new List<VFXExpression>();
            var sampling_A = SampleVertexAttribute(source, sampledIndex_A, vertexAttributes).ToArray();
            var sampling_B = SampleVertexAttribute(source, sampledIndex_B, vertexAttributes).ToArray();
            var sampling_C = SampleVertexAttribute(source, sampledIndex_C, vertexAttributes).ToArray();

            VFXExpression barycentricCoordinates = null;
            var one = VFXOperatorUtility.OneExpression[UnityEngine.VFX.VFXValueType.Float];
            if (coordMode == SurfaceCoordinates.Barycentric)
            {
                var barycentricCoordinateInput = coord;
                barycentricCoordinates = new VFXExpressionCombine(barycentricCoordinateInput.x, barycentricCoordinateInput.y, one - barycentricCoordinateInput.x - barycentricCoordinateInput.y);
            }
            else if (coordMode == SurfaceCoordinates.Uniform)
            {
                //https://hal.archives-ouvertes.fr/hal-02073696v2/document
                var input = coord;

                var half2 = VFXOperatorUtility.HalfExpression[UnityEngine.VFX.VFXValueType.Float2];
                var zero = VFXOperatorUtility.ZeroExpression[UnityEngine.VFX.VFXValueType.Float];
                var t = input * half2;
                var offset = t.y - t.x;
                var pred = new VFXExpressionCondition(UnityEngine.VFX.VFXValueType.Float, VFXCondition.Greater, offset, zero);
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

            for (int i = 0; i < vertexAttributes.Count(); ++i)
            {
                var outputValueType = sampling_A[i].valueType;

                var barycentricCoordinateX = VFXOperatorUtility.CastFloat(barycentricCoordinates.x, outputValueType);
                var barycentricCoordinateY = VFXOperatorUtility.CastFloat(barycentricCoordinates.y, outputValueType);
                var barycentricCoordinateZ = VFXOperatorUtility.CastFloat(barycentricCoordinates.z, outputValueType);

                var r = sampling_A[i] * barycentricCoordinateX + sampling_B[i] * barycentricCoordinateY + sampling_C[i] * barycentricCoordinateZ;
                yield return r;
            }
        }

        public static IEnumerable<VFXExpression> SampleTriangleAttribute(VFXExpression source, VFXExpression triangleIndex, VFXExpression coord, VFXOperatorUtility.SequentialAddressingMode mode, SurfaceCoordinates coordMode, IEnumerable<VertexAttribute> vertexAttributes)
        {
            bool skinnedMesh = source.valueType == UnityEngine.VFX.VFXValueType.SkinnedMeshRenderer;
            var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);
            var UintThree = VFXOperatorUtility.ThreeExpression[UnityEngine.VFX.VFXValueType.Uint32];

            var meshIndexCount = new VFXExpressionMeshIndexCount(mesh);
            var triangleCount = meshIndexCount / UintThree;
            triangleIndex = VFXOperatorUtility.ApplyAddressingMode(triangleIndex, triangleCount, mode);

            return SampleTriangleAttribute(source, triangleIndex, coord, coordMode, vertexAttributes);
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] outputExpressions = null;
            if (placementMode == PlacementMode.Vertex)
            {
                var sampled = SampleVertexAttribute(inputExpression[0], inputExpression[1], mode, GetOutputVertexAttributes());
                outputExpressions = sampled.ToArray();
            }
            else if (placementMode == PlacementMode.Edge)
            {
                var sampled = SampleEdgeAttribute(inputExpression[0], inputExpression[1], inputExpression[2], mode, GetOutputVertexAttributes());
                outputExpressions = sampled.ToArray();
            }
            else if (placementMode == PlacementMode.Surface)
            {
                var sampled = SampleTriangleAttribute(inputExpression[0], inputExpression[1], inputExpression[2], mode, surfaceCoordinates, GetOutputVertexAttributes());
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
