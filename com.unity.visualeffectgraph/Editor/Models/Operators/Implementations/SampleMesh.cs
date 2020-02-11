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
            [Tooltip("The mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
        }

        public class ConstantInputProperties
        {
            [Tooltip("Sets the value used when determining the random number. Using the same seed results in the same random number every time.")]
            public uint seed = 0u;
        }

        public class InputPropertiesVertex
        {
            [Tooltip("The vertex index to read from.")]
            public uint vertex = 0u;
        }

        //public enum PlacementMode
        //{
        //    Vertex,
        //    Edge,
        //    Surface
        //};

        public enum SelectionMode
        {
            Random,
            Custom
        };

        [Flags]
        public enum VertexAttributeFlag
        {
            None = 0,
            Position = 1 << 0,
            Normal = 1 << 2,
            Tangent = 1 << 3,
            Color = 1 << 4,
            TexCoord0 = 1 << 5,
            TexCoord1 = 1 << 6,
            TexCoord2 = 1 << 7,
            TexCoord3 = 1 << 8,
            TexCoord4 = 1 << 9,
            TexCoord5 = 1 << 10,
            TexCoord6 = 1 << 11,
            TexCoord7 = 1 << 12,
            BlendWeight = 1 << 13,
            BlendIndices = 1 << 14
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies read output during mesh sampling")]
        private VertexAttributeFlag output = VertexAttributeFlag.Position | VertexAttributeFlag.Color | VertexAttributeFlag.TexCoord0;

        [VFXSetting, SerializeField, Tooltip("Specifies the selection mode, embedded random or custom index sampling")]
        private SelectionMode selection = SelectionMode.Random;

        [VFXSetting, SerializeField, Tooltip("Specifies whether the random number is generated for each particle, each particle strip, or is shared by the whole system.")]
        private VFXSeedMode seed = VFXSeedMode.PerParticle;

        [VFXSetting, SerializeField, Tooltip("When enabled, the random number will remain constant. Otherwise, it will change every time it is evaluated.")]
        private bool constant = true;

        [VFXSetting, SerializeField, Tooltip("Change how the out of bounds are handled while fetching with the custom vertex index.")]
        private VFXOperatorUtility.SequentialAddressingMode adressingMode = VFXOperatorUtility.SequentialAddressingMode.Wrap;

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

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = PropertiesFromType("InputProperties");

                if (/*Placement == PlacementMode.Vertex && */selection == SelectionMode.Custom)
                    properties = properties.Concat(PropertiesFromType("InputPropertiesVertex"));

                if (selection == SelectionMode.Random && (constant || seed == VFXSeedMode.PerParticleStrip))
                    properties = properties.Concat(PropertiesFromType("ConstantInputProperties"));

                return properties;
            }
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
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "count", new VFXPropertyAttribute(VFXPropertyAttribute.Type.kTooltip, "The number of vertices in this mesh")));
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var s in base.filteredOutSettings)
                    yield return s;

                if (seed == VFXSeedMode.PerParticleStrip || selection != SelectionMode.Random)
                    yield return "constant";

                if (selection != SelectionMode.Random)
                    yield return "seed";

                if (selection != SelectionMode.Custom)
                    yield return "adressingMode";
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var mesh = inputExpression[0];

            VFXExpression meshVertexStride = new VFXExpressionMeshVertexStride(mesh);
            VFXExpression meshVertexCount = new VFXExpressionMeshVertexCount(mesh);

            VFXExpression vertexIndex;
            switch (selection)
            {
                case SelectionMode.Random:
                    {
                        var rand = VFXOperatorUtility.BuildRandom(seed, constant, inputExpression.Length > 1 ? inputExpression[1] : null);
                        vertexIndex = rand * new VFXExpressionCastUintToFloat(meshVertexCount);
                        vertexIndex = new VFXExpressionCastFloatToUint(vertexIndex);
                    }
                    break;
                case SelectionMode.Custom:
                    {
                        vertexIndex = VFXOperatorUtility.ApplyAddressingMode(inputExpression[1], meshVertexCount, adressingMode);
                    }
                    break;
                default:
                    throw new NotImplementedException("Unhandled Selection Mode");
            }

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

            outputExpressions.Add(meshVertexCount);
            return outputExpressions.ToArray();
        }
    }
}
