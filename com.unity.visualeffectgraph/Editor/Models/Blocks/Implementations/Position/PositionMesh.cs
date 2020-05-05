using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position", experimental = true)]
    class PositionMesh : PositionBase
    {
        [VFXSetting, SerializeField, Tooltip("Change how the out of bounds are handled while fetching with the custom vertex index.")]
        private VFXOperatorUtility.SequentialAddressingMode adressingMode = VFXOperatorUtility.SequentialAddressingMode.Wrap;

        public override string name { get { return "Position (Mesh)"; } }

        public class CustomPropertiesMesh
        {
            [Tooltip("Sets the mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
        }

        public class CustomPropertiesVertex
        {
            [Tooltip("Sets the vertex index to read from.")]
            public uint vertex = 0;
        }

        protected override bool needDirectionWrite { get { return true; } }
        protected override bool supportsVolumeSpawning { get { return false; } }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression mesh = null;
                VFXExpression inputVertex = null;
                foreach (var parameter in base.parameters)
                {
                    if (parameter.name == "mesh")
                        mesh = parameter.exp;
                    else if (parameter.name == "vertex")
                        inputVertex = parameter.exp;
                    else
                        yield return parameter;
                }

                var meshVertexCount = new VFXExpressionMeshVertexCount(mesh);
                VFXExpression vertexIndex;
                if (spawnMode == SpawnMode.Custom)
                {
                    vertexIndex = VFXOperatorUtility.ApplyAddressingMode(inputVertex, meshVertexCount, adressingMode);
                }
                else //if(spawnMode == SpawnMode.Random)
                {
                    var rand = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false);
                    vertexIndex = new VFXExpressionCastFloatToUint(rand * new VFXExpressionCastUintToFloat(meshVertexCount));
                }

                var positionOffset = new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Position));
                var normalOffset = new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Normal));

#if UNITY_2020_2_OR_NEWER

                var vertexStridePosition = new VFXExpressionMeshVertexStride(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Position));
                var vertexStrideNormal = new VFXExpressionMeshVertexStride(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Normal));

                var positionVertexOffset = vertexStridePosition * vertexIndex + positionOffset;
                var normalVertexOffset = vertexStrideNormal * vertexIndex + normalOffset;
                var positionChannelFormatAndDimension = new VFXExpressionMeshChannelFormatAndDimension(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Position));
                var normalChannelFormatAndDimension = new VFXExpressionMeshChannelFormatAndDimension(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Normal));

                yield return new VFXNamedExpression(new VFXExpressionSampleMeshFloat3(mesh, positionVertexOffset, positionChannelFormatAndDimension), "sampledPosition");
                yield return new VFXNamedExpression(new VFXExpressionSampleMeshFloat3(mesh, normalVertexOffset, normalChannelFormatAndDimension), "sampledNormal");
#else
                var vertexStride = new VFXExpressionMeshVertexStride(mesh);
                yield return new VFXNamedExpression(new VFXExpressionSampleMeshFloat3(mesh, vertexIndex, positionOffset, vertexStride), "sampledPosition");
                yield return new VFXNamedExpression(new VFXExpressionSampleMeshFloat3(mesh, vertexIndex, normalOffset, vertexStride), "sampledNormal");
#endif
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                if (spawnMode != SpawnMode.Custom)
                    yield return "adressingMode";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;

                properties = properties.Concat(PropertiesFromType("CustomPropertiesMesh"));

                if (/*Placement == PlacementMode.Vertex &&*/ spawnMode == SpawnMode.Custom)
                    properties = properties.Concat(PropertiesFromType("CustomPropertiesVertex"));

                return properties;
            }
        }

        public override string source
        {
            get
            {
                string source = "";
                source += @"
position = sampledPosition;
direction = sampledNormal;";
                return source;
            }
        }
    }
}
