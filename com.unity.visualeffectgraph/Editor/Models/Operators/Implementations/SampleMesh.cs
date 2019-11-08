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

        public class InputPropertiesVertex
        {
            [Tooltip("The vertex index to read from.")]
            public uint vertex = 0;
        }

        public enum PlacementMode
        {
            Vertex,
            Edge,
            Surface
        };

        public enum SelectionMode
        {
            Random,
            Custom
        };

        //[VFXSetting, SerializeField] // TODO - support surface sampling
        private PlacementMode Placement = PlacementMode.Vertex;

        [VFXSetting, SerializeField]
        private SelectionMode Selection = SelectionMode.Random;

        // TODO: support flags/mask UI, for outputting multiple attributes from one operator
        [VFXSetting, SerializeField]
        private VertexAttribute Output = VertexAttribute.Position;

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = PropertiesFromType("InputProperties");

                if (Placement == PlacementMode.Vertex && Selection == SelectionMode.Custom)
                    properties = properties.Concat(PropertiesFromType("InputPropertiesVertex"));

                return properties;
            }
        }

        private Type GetOutputType()
        {
            switch (Output)
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
                if (Placement != PlacementMode.Vertex)
                    yield return "Selection";
               
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var mesh = inputExpression[0];

            VFXExpression meshVertexStride = new VFXExpressionMeshVertexStride(mesh);
            VFXExpression meshChannelOffset = new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<uint>((uint)Output));
            VFXExpression meshVertexCount = new VFXExpressionMeshVertexCount(mesh);

            //if (Placement == PlacementMode.Vertex)
            {
                VFXExpression vertexIndex;
                switch (Selection)
                {
                    case SelectionMode.Random:
                        {
                            var rand = new VFXExpressionRandom(true);
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
                if (Output == VertexAttribute.Color)
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
