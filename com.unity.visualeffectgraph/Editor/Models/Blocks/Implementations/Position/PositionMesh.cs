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
                foreach (var parameter in base.parameters)
                    yield return parameter;

                var mesh = inputSlots[0].GetExpression();

                var meshVertexCount = new VFXExpressionMeshVertexCount(mesh);
                VFXExpression vertexIndex;
                if (spawnMode == SpawnMode.Custom)
                {
                    vertexIndex = VFXOperatorUtility.ApplyAddressingMode(base.parameters.First(o => o.name == "vertex").exp, meshVertexCount, adressingMode);
                }
                else //if(spawnMode == SpawnMode.Random)
                {
                    var rand = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false);
                    vertexIndex = new VFXExpressionCastFloatToUint(rand * new VFXExpressionCastUintToFloat(meshVertexCount));
                }

                var vertexStride = new VFXExpressionMeshVertexStride(mesh);
                var positionOffset = new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Position));
                var normalOffset = new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Normal));

#if UNITY_2020_2_OR_NEWER
                yield return new VFXNamedExpression(vertexStride * vertexIndex + positionOffset, "positionVertexOffset");
                yield return new VFXNamedExpression(vertexStride * vertexIndex + normalOffset, "normalVertexOffset");
                yield return new VFXNamedExpression(new VFXExpressionMeshChannelFormatAndDimension(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Position)), "positionChannelFormatAndDimension");
                yield return new VFXNamedExpression(new VFXExpressionMeshChannelFormatAndDimension(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Normal)), "normalChannelFormatAndDimension");
#else
                yield return new VFXNamedExpression(vertexIndex, "vertexIndex");
                yield return new VFXNamedExpression(vertexStride, "meshVertexStride");
                yield return new VFXNamedExpression(positionOffset, "meshPositionOffset");
                yield return new VFXNamedExpression(normalOffset, "meshNormalOffset");
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

#if UNITY_2020_2_OR_NEWER
                source += @"
position = SampleMeshFloat3(mesh, positionVertexOffset, positionChannelFormatAndDimension);
direction = SampleMeshFloat3(mesh, normalVertexOffset, normalChannelFormatAndDimension);";
#else
                source += @"
position = SampleMeshFloat3(mesh, vertexIndex, meshPositionOffset, meshVertexStride);
direction = SampleMeshFloat3(mesh, vertexIndex, meshNormalOffset, meshVertexStride);";
#endif
                return source;
            }
        }
    }
}
