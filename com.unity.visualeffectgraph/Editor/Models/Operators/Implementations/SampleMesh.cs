using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling")]
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

        //[VFXSetting, SerializeField] // TODO - support surface sampling
        //private PlacementMode Placement = PlacementMode.Vertex;

        // TODO: support flags/mask UI, for outputting multiple attributes from one operator
        [VFXSetting, SerializeField, Tooltip("Specifies read output during mesh sampling")]
        private VertexAttribute output = VertexAttribute.Position;

        [VFXSetting, SerializeField, Tooltip("Specifies the selection mode, embedded random or custom index sampling")]
        private SelectionMode selection = SelectionMode.Random;

        [VFXSetting, SerializeField, Tooltip("Specifies whether the random number is generated for each particle, each particle strip, or is shared by the whole system.")]
        private VFXSeedMode seed = VFXSeedMode.PerParticle;
        [VFXSetting, SerializeField, Tooltip("When enabled, the random number will remain constant. Otherwise, it will change every time it is evaluated.")]
        private bool constant = true;

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
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var mesh = inputExpression[0];

            VFXExpression meshVertexStride = new VFXExpressionMeshVertexStride(mesh);
            VFXExpression meshChannelOffset = new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<uint>((uint)output));
            VFXExpression meshVertexCount = new VFXExpressionMeshVertexCount(mesh);

            //if (Placement == PlacementMode.Vertex)
            {
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
                            vertexIndex = VFXOperatorUtility.Modulo(inputExpression[1], meshVertexCount);
                        }
                        break;
                    default:
                        throw new NotImplementedException("Unhandled Selection Mode");
                }

                var outputType = GetOutputType();
                if (output == VertexAttribute.Color)
                    return new[] { new VFXExpressionSampleMeshColor(mesh, vertexIndex, meshChannelOffset, meshVertexStride) };
                if (outputType == typeof(float))
                    return new[] { new VFXExpressionSampleMeshFloat(mesh, vertexIndex, meshChannelOffset, meshVertexStride) };
                else if (outputType == typeof(Vector2))
                    return new[] { new VFXExpressionSampleMeshFloat2(mesh, vertexIndex, meshChannelOffset, meshVertexStride) };
                else if (outputType == typeof(Vector3))
                    return new[] { new VFXExpressionSampleMeshFloat3(mesh, vertexIndex, meshChannelOffset, meshVertexStride) };
                else
                    return new[] { new VFXExpressionSampleMeshFloat4(mesh, vertexIndex, meshChannelOffset, meshVertexStride) };
            }
            /*else
            {
                // todo: Triangle
            }*/
        }
    }
}
