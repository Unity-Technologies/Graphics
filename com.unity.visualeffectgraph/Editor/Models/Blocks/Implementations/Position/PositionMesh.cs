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
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on Position. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode compositionPosition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on Direction. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode compositionDirection = AttributeCompositionMode.Overwrite;

        [VFXSetting, SerializeField, Tooltip("Specifies how Unity handles the sample when the custom vertex index is out the out of bounds of the vertex array.")]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Clamp;

        public override string name { get { return "Position (Mesh)"; } }

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

        public class CustomPropertiesBlendPosition
        {
            [Range(0.0f, 1.0f), Tooltip("Set the blending value for position attribute.")]
            public float blendPosition;
        }

        public class CustomPropertiesBlendDirection
        {
            [Range(0.0f, 1.0f), Tooltip("Set the blending value for direction attribute.")]
            public float blendDirection;
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
                    vertexIndex = VFXOperatorUtility.ApplyAddressingMode(inputVertex, meshVertexCount, mode);
                }
                else //if(spawnMode == SpawnMode.Random)
                {
                    var rand = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false);
                    vertexIndex = new VFXExpressionCastFloatToUint(rand * new VFXExpressionCastUintToFloat(meshVertexCount));
                }

                var positionOffset = new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Position));
                var normalOffset = new VFXExpressionMeshChannelOffset(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Normal));

                var vertexStridePosition = new VFXExpressionMeshVertexStride(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Position));
                var vertexStrideNormal = new VFXExpressionMeshVertexStride(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Normal));

                var positionVertexOffset = vertexStridePosition * vertexIndex + positionOffset;
                var normalVertexOffset = vertexStrideNormal * vertexIndex + normalOffset;
                var positionChannelFormatAndDimension = new VFXExpressionMeshChannelInfos(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Position));
                var normalChannelFormatAndDimension = new VFXExpressionMeshChannelInfos(mesh, VFXValue.Constant<UInt32>((UInt32)VertexAttribute.Normal));

                yield return new VFXNamedExpression(new VFXExpressionSampleMeshFloat3(mesh, positionVertexOffset, positionChannelFormatAndDimension), "readPosition");
                yield return new VFXNamedExpression(new VFXExpressionSampleMeshFloat3(mesh, normalVertexOffset, normalChannelFormatAndDimension), "readDirection");
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

                if (compositionPosition == AttributeCompositionMode.Blend)
                    properties = properties.Concat(PropertiesFromType("CustomPropertiesBlendPosition"));

                if (compositionDirection == AttributeCompositionMode.Blend)
                    properties = properties.Concat(PropertiesFromType("CustomPropertiesBlendDirection"));

                return properties;
            }
        }

        public override string source
        {
            get
            {
                string source = "";
                source += "\n" + VFXBlockUtility.GetComposeString(compositionPosition, "position", "readPosition", "blendPosition");
                source += "\n" + VFXBlockUtility.GetComposeString(compositionDirection, "direction", "readDirection", "blendDirection");
                return source;
            }
        }
    }
}
