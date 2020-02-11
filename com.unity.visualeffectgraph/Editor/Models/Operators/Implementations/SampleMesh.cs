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

        // TODO: support flags/mask UI, for outputting multiple attributes from one operator
        [VFXSetting, SerializeField, Tooltip("Specifies read output during mesh sampling")]
        private VertexAttribute output = VertexAttribute.Position;

        [VFXSetting, SerializeField, Tooltip("Specifies the selection mode, embedded random or custom index sampling")]
        private SelectionMode selection = SelectionMode.Random;

        [VFXSetting, SerializeField, Tooltip("Specifies whether the random number is generated for each particle, each particle strip, or is shared by the whole system.")]
        private VFXSeedMode seed = VFXSeedMode.PerParticle;

        [VFXSetting, SerializeField, Tooltip("When enabled, the random number will remain constant. Otherwise, it will change every time it is evaluated.")]
        private bool constant = true;

        [VFXSetting, SerializeField, Tooltip("Change how the out of bounds are handled while fetching with the custom vertex index.")]
        private VFXOperatorUtility.SequentialAddressingMode adressingMode = VFXOperatorUtility.SequentialAddressingMode.Wrap;

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

        private Type GetOutputType()
        {
            switch (output)
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
                case VertexAttribute.TexCoord7: return typeof(Vector2);
                case VertexAttribute.BlendWeight: return typeof(Vector4);
                case VertexAttribute.BlendIndices: return typeof(Vector4);
                default:
                    throw new NotImplementedException("Unhandled VertexAttribute");
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetOutputType(), ""));
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
            VFXExpression meshChannelOffset = new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<uint>((uint)output));
            VFXExpression meshVertexCount = new VFXExpressionMeshVertexCount(mesh);

            VFXExpression vertexIndex;
            switch (selection)
            {
                case SelectionMode.Random:
                    {
                        var rand = VFXOperatorUtility.BuildRandom(seed, constant, inputExpression[1]);
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

            var outputType = GetOutputType();
            VFXExpression sampled = null;
            if (output == VertexAttribute.Color)
                sampled = new VFXExpressionSampleMeshColor(mesh, vertexIndex, meshChannelOffset, meshVertexStride);
            else if (outputType == typeof(float))
                sampled = new VFXExpressionSampleMeshFloat(mesh, vertexIndex, meshChannelOffset, meshVertexStride);
            else if (outputType == typeof(Vector2))
                sampled = new VFXExpressionSampleMeshFloat2(mesh, vertexIndex, meshChannelOffset, meshVertexStride);
            else if (outputType == typeof(Vector3))
                sampled = new VFXExpressionSampleMeshFloat3(mesh, vertexIndex, meshChannelOffset, meshVertexStride);
            else
                sampled = new VFXExpressionSampleMeshFloat4(mesh, vertexIndex, meshChannelOffset, meshVertexStride);
            return new[] { sampled, meshVertexCount };
        }
    }
}
