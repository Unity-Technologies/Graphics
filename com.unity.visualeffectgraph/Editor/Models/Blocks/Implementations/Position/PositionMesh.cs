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
            public int vertex = 0;
        }

        public enum PlacementMode
        {
            Vertex,
            Edge,
            Surface
        };

        //[VFXSetting] // TODO - support surface sampling
        public PlacementMode Placement = PlacementMode.Vertex;

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
                yield return new VFXNamedExpression(new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<int>((int)VertexAttribute.Position)), "meshPositionOffset");
                yield return new VFXNamedExpression(new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<int>((int)VertexAttribute.Normal)), "meshNormalOffset");
                yield return new VFXNamedExpression(new VFXExpressionCastIntToFloat(new VFXExpressionMeshVertexCount(mesh)), "meshVertexCount");
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;

                properties = properties.Concat(PropertiesFromType("CustomPropertiesMesh"));

                if (Placement == PlacementMode.Vertex && spawnMode == SpawnMode.Custom)
                    properties = properties.Concat(PropertiesFromType("CustomPropertiesVertex"));

                return properties;
            }
        }

        public override string source
        {
            get
            {
                string source = "";

                if (Placement == PlacementMode.Vertex)
                {

                    switch (spawnMode)
                    {
                        case SpawnMode.Randomized:
                            {
                                source += @"int vertexIndex = (int)fmod(RAND * meshVertexCount, meshVertexCount);";
                            }
                            break;
                        case SpawnMode.Custom:
                            {
                                source += @"int vertexIndex = (vertex % meshVertexCount);";
                            }
                            break;
                        default:
                            throw new NotImplementedException("Unhandled Selection Mode");
                    }

                    source += @"
position = SampleMeshFloat3(mesh, (int)vertexIndex, meshPositionOffset, meshVertexStride);
direction = SampleMeshFloat3(mesh, (int)vertexIndex, meshNormalOffset, meshVertexStride);";
                }
                else
                {
                    // TODO
                }

                return source;
            }
        }
    }
}
