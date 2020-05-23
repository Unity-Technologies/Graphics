using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.VFX.Operator;

namespace UnityEditor.VFX.Block
{
    class PositionMeshProvider : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "sourceMesh", Enum.GetValues(typeof(SampleMesh.SourceType)).Cast<object>().ToArray() },
                };
            }
        }
    }

    [VFXInfo(category = "Position", variantProvider = typeof(PositionMeshProvider), experimental = true)]
    class PositionMesh : PositionBase
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on Position. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode compositionPosition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on Direction. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode compositionDirection = AttributeCompositionMode.Overwrite;

        [VFXSetting, SerializeField, Tooltip("Specifies how Unity handles the sample when the custom vertex index is out the out of bounds of the vertex array.")]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Clamp;

        [VFXSetting, SerializeField, Tooltip("Change what kind of primitive we want to sample.")]
        private SampleMesh.PlacementMode placementMode = SampleMesh.PlacementMode.Vertex;

        //[VFXSetting, SerializeField, Tooltip("Surface sampling coordinate.")]
        //private SampleMesh.SurfaceCoordinates surfaceCoordinates = SampleMesh.SurfaceCoordinates.Uniform;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Choose between classic mesh sampling or skinned renderer mesh sampling.")]
        private SampleMesh.SourceType sourceMesh = SampleMesh.SourceType.Mesh;

        public override string name
        {
            get
            {
                if (sourceMesh == SampleMesh.SourceType.Mesh)
                    return "Position (Mesh)";
                else
                    return "Position (Skinned Mesh)";
            }
        }

        public class CustomPropertiesMesh
        {
            [Tooltip("Sets the Mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
        }

        public class CustomPropertiesPropertiesSkinnedMeshRenderer
        {
            [Tooltip("Sets the Mesh to sample from, has to be an exposed entry.")]
            public SkinnedMeshRenderer skinnedMesh = null;
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

        private static VFXExpression BuildRandomUIntPerParticle(VFXExpression max)
        {
            //TODO : Add support of proper integer random
            var rand = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false);
            VFXExpression r = new VFXExpressionCastFloatToUint(rand * new VFXExpressionCastUintToFloat(max));
            r = VFXOperatorUtility.ApplyAddressingMode(r, max, VFXOperatorUtility.SequentialAddressingMode.Clamp);
            return r;
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression source = null;
                VFXExpression inputVertex = null;
                foreach (var parameter in base.parameters)
                {
                    if (parameter.name == "mesh" || parameter.name == "skinnedMesh")
                        source = parameter.exp;
                    else if (parameter.name == "vertex")
                        inputVertex = parameter.exp;
                    else
                        yield return parameter;
                }
                bool skinnedMesh = source.valueType == UnityEngine.VFX.VFXValueType.SkinnedMeshRenderer;
                var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);
                var meshVertexCount = new VFXExpressionMeshVertexCount(mesh);
                var meshIndexCount = new VFXExpressionMeshIndexCount(mesh);

                VFXExpression vertexIndex;
                if (spawnMode == SpawnMode.Custom)
                {
                    vertexIndex = VFXOperatorUtility.ApplyAddressingMode(inputVertex, meshVertexCount, mode);
                }
                else //if(spawnMode == SpawnMode.Random)
                {
                    vertexIndex = BuildRandomUIntPerParticle(meshVertexCount);
                }

                var vertexAttributes = new[] { VertexAttribute.Position, VertexAttribute.Normal };
                var sampling = SampleMesh.SampleVertexAttribute(source, vertexIndex, vertexAttributes).ToArray();

                yield return new VFXNamedExpression(sampling[0], "readPosition");
                yield return new VFXNamedExpression(sampling[1], "readDirection");
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

                if (sourceMesh == SampleMesh.SourceType.Mesh)
                    properties = properties.Concat(PropertiesFromType("CustomPropertiesMesh"));
                else
                    properties = properties.Concat(PropertiesFromType("CustomPropertiesPropertiesSkinnedMeshRenderer"));

                if (placementMode == SampleMesh.PlacementMode.Vertex && spawnMode == SpawnMode.Custom)
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
