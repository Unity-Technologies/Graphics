using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling", experimental = true)]
    class SampleMesh : VFXOperator
    {
        override public string name { get { return "Sample Mesh"; } }

        public class InputProperties
        {
            [Tooltip("Sets the Mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
            [Tooltip("Sets the vertex index to read from.")]
            public uint vertex = 0u;
        }

        //public enum PlacementMode
        //{
        //    Vertex,
        //    Edge,
        //    Surface
        //};

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

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Outputs the result of the Mesh sampling operation.")]
        private VertexAttributeFlag output = VertexAttributeFlag.Position | VertexAttributeFlag.Color | VertexAttributeFlag.TexCoord0;

        [VFXSetting, SerializeField, Tooltip("Specifies how Unity handles the sample when the custom vertex index is out the out of bounds of the vertex array.")]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Clamp;

        private bool HasOutput(VertexAttributeFlag flag)
        {
            return (output & flag) == flag;
        }

        private IEnumerable<VertexAttributeFlag> GetOutputVertexAttributes()
        {
            var vertexAttributes = Enum.GetValues(typeof(VertexAttributeFlag)).Cast<VertexAttributeFlag>();
            foreach (var vertexAttribute in vertexAttributes)
                if (vertexAttribute != VertexAttributeFlag.None && HasOutput(vertexAttribute))
                    yield return vertexAttribute;
        }

        private static Type GetOutputType(VertexAttributeFlag attribute)
        {
            switch (attribute)
            {
                case VertexAttributeFlag.Position: return typeof(Vector3);
                case VertexAttributeFlag.Normal: return typeof(Vector3);
                case VertexAttributeFlag.Tangent: return typeof(Vector4);
                case VertexAttributeFlag.Color: return typeof(Vector4);
                case VertexAttributeFlag.TexCoord0:
                case VertexAttributeFlag.TexCoord1:
                case VertexAttributeFlag.TexCoord2:
                case VertexAttributeFlag.TexCoord3:
                case VertexAttributeFlag.TexCoord4:
                case VertexAttributeFlag.TexCoord5:
                case VertexAttributeFlag.TexCoord6:
                case VertexAttributeFlag.TexCoord7: return typeof(Vector2);
                case VertexAttributeFlag.BlendWeight: return typeof(Vector4);
                case VertexAttributeFlag.BlendIndices: return typeof(Vector4);
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

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var mesh = inputExpression[0];

            var meshVertexStride = new VFXExpressionMeshVertexStride(mesh);
            var meshVertexCount = new VFXExpressionMeshVertexCount(mesh);
            var vertexIndex = VFXOperatorUtility.ApplyAddressingMode(inputExpression[1], meshVertexCount, mode);

            var outputExpressions = new List<VFXExpression>();
            foreach (var vertexAttribute in GetOutputVertexAttributes())
            {
                var meshChannelOffset = new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<uint>((uint)GetActualVertexAttribute(vertexAttribute)));

                var outputType = GetOutputType(vertexAttribute);
                VFXExpression sampled = null;
                if (vertexAttribute == VertexAttributeFlag.Color)
                    sampled = new VFXExpressionSampleMeshColor(mesh, vertexIndex, meshChannelOffset, meshVertexStride);
                else if (outputType == typeof(float))
                    sampled = new VFXExpressionSampleMeshFloat(mesh, vertexIndex, meshChannelOffset, meshVertexStride);
                else if (outputType == typeof(Vector2))
                    sampled = new VFXExpressionSampleMeshFloat2(mesh, vertexIndex, meshChannelOffset, meshVertexStride);
                else if (outputType == typeof(Vector3))
                    sampled = new VFXExpressionSampleMeshFloat3(mesh, vertexIndex, meshChannelOffset, meshVertexStride);
                else
                    sampled = new VFXExpressionSampleMeshFloat4(mesh, vertexIndex, meshChannelOffset, meshVertexStride);
                outputExpressions.Add(sampled);
            }
            return outputExpressions.ToArray();
        }
    }
}
