using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position", experimental = true, variantProvider = typeof(PositionBaseProvider))]
    class PositionMesh : PositionBase
    {

        [VFXSetting, SerializeField, Tooltip("Specifies how Unity handles the sample when the custom vertex index is out the out of bounds of the vertex array.")]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Clamp;

        public override string name { get { return string.Format(base.name, "Mesh"); ; } }

        public class CustomPropertiesMesh
        {
            [Tooltip("Sets the Mesh to sample from.")]
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

                yield return new VFXNamedExpression(new VFXExpressionMeshVertexStride(mesh), "meshVertexStride");
                yield return new VFXNamedExpression(new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Position)), "meshPositionOffset");
                yield return new VFXNamedExpression(new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Normal)), "meshNormalOffset");
                var meshVertexCount = new VFXExpressionMeshVertexCount(mesh);
                VFXExpression vertexIndex;
                if (spawnMode == SpawnMode.Custom)
                {
                    vertexIndex = VFXOperatorUtility.ApplyAddressingMode(base.parameters.First(o => o.name == "vertex").exp, meshVertexCount, mode);
                }
                else //if(spawnMode == SpawnMode.Random)
                {
                    var rand = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false, new RandId(this));
                    vertexIndex = new VFXExpressionCastFloatToUint(rand * new VFXExpressionCastUintToFloat(meshVertexCount));
                }
                yield return new VFXNamedExpression(vertexIndex, "vertexIndex");
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                if (spawnMode != SpawnMode.Custom)
                    yield return "mode";
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
float3 readPosition = SampleMeshFloat3(mesh, vertexIndex, meshPositionOffset, meshVertexStride);
float3 readDirection = SampleMeshFloat3(mesh, vertexIndex, meshNormalOffset, meshVertexStride);";
                source += "\n" + VFXBlockUtility.GetComposeString(compositionPosition, "position", "readPosition", "blendPosition");
                source += "\n" + VFXBlockUtility.GetComposeString(compositionDirection, "direction", "readDirection", "blendDirection");
                return source;
            }
        }
    }
}
