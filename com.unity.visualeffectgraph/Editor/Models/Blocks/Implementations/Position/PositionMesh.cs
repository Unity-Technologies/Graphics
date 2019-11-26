using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionMesh : PositionBase
    {
        public override string name { get { return "Position (Mesh)"; } }

        public class CustomPropertiesMesh
        {
            [Tooltip("The mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
        }

        public class CustomPropertiesVertex
        {
            [Tooltip("The vertex index to read from.")]
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

                yield return new VFXNamedExpression(new VFXExpressionMeshVertexStride(mesh), "meshVertexStride");
                yield return new VFXNamedExpression(new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Position)), "meshPositionOffset");
                yield return new VFXNamedExpression(new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Normal)), "meshNormalOffset");
                var meshVertexCount = new VFXExpressionMeshVertexCount(mesh);
                VFXExpression vertexIndex;
                if (spawnMode == SpawnMode.Custom)
                {
                    vertexIndex = VFXOperatorUtility.Modulo(base.parameters.First(o => o.name == "vertex").exp, meshVertexCount);
                }
                else //if(spawnMode == SpawnMode.Random)
                {
                    var rand = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false);
                    vertexIndex = new VFXExpressionCastFloatToUint(rand * new VFXExpressionCastUintToFloat(meshVertexCount));
                }
                yield return new VFXNamedExpression(vertexIndex, "vertexIndex");
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
position = SampleMeshFloat3(mesh, vertexIndex, meshPositionOffset, meshVertexStride);
direction = SampleMeshFloat3(mesh, vertexIndex, meshNormalOffset, meshVertexStride);";

                return source;
            }
        }
    }
}
