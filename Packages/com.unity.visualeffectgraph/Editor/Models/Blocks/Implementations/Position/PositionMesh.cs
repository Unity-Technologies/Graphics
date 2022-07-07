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
        [VFXSetting, SerializeField, Tooltip("Specifies how Unity handles the sample when the custom vertex index is out the out of bounds of the vertex array.")]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Clamp;

        [VFXSetting, SerializeField, Tooltip("Specifies which primitive part of the mesh to sample from.")]
        private SampleMesh.PlacementMode placementMode = SampleMesh.PlacementMode.Vertex;

        [VFXSetting, SerializeField, Tooltip("Specifies how to sample the surface of a triangle.")]
        private SampleMesh.SurfaceCoordinates surfaceCoordinates = SampleMesh.SurfaceCoordinates.Uniform;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the kind of geometry to sample from.")]
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
            [Tooltip("Specifies the Mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
        }

        public class CustomPropertiesPropertiesSkinnedMeshRenderer
        {
            [Tooltip("Specifies the Skinned Mesh Renderer component to sample from. The Skinned Mesh Renderer has to be an exposed entry.")]
            public SkinnedMeshRenderer skinnedMesh = null;
        }

        public class CustomPropertiesVertex
        {
            [Tooltip("Sets the vertex index to read from.")]
            public uint vertex = 0;
        }

        public class CustomPropertiesEdge
        {
            [Tooltip("Sets the start index of the edge. The Block uses this index and the next index to render the line.")]
            public uint index = 0u;

            [Range(0, 1), Tooltip("Controls the percentage along the edge, from the start position to the end position, that the sample position is taken.")]
            public float edge;
        }

        public class CustomPropertiesPlacementSurfaceBarycentricCoordinates
        {
            [Tooltip("The triangle index to read from.")]
            public uint triangle = 0u;

            [Tooltip("Barycentric coordinate (z is computed from x & y).")]
            public Vector2 barycentric;
        }

        public class CustomPropertiesPlacementSurfaceLowDistorsionMapping
        {
            [Tooltip("The triangle index to read from.")]
            public uint triangle = 0u;

            [Tooltip("The uniform coordinate to sample the triangle at. The Block takes this value and maps it from a square coordinate to triangle space.")]
            public Vector2 square;
        }

        protected override bool needDirectionWrite { get { return true; } }
        protected override bool supportsVolumeSpawning { get { return false; } }

        private VFXExpression BuildRandomUIntPerParticle(VFXExpression max, int id)
        {
            //TODO : Add support of proper integer random
            var rand = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false, new RandId(this, id));
            VFXExpression r = new VFXExpressionCastFloatToUint(rand * new VFXExpressionCastUintToFloat(max));
            r = VFXOperatorUtility.ApplyAddressingMode(r, max, VFXOperatorUtility.SequentialAddressingMode.Clamp);
            return r;
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression source = null;
                VFXExpression index = null;
                VFXExpression coordinate = null;
                foreach (var parameter in base.parameters)
                {
                    if (parameter.name == nameof(CustomPropertiesMesh.mesh)
                        || parameter.name == nameof(CustomPropertiesPropertiesSkinnedMeshRenderer.skinnedMesh))
                        source = parameter.exp;
                    else if (parameter.name == nameof(CustomPropertiesEdge.edge)
                             || parameter.name == nameof(CustomPropertiesPlacementSurfaceLowDistorsionMapping.square)
                             || parameter.name == nameof(CustomPropertiesPlacementSurfaceBarycentricCoordinates.barycentric))
                        coordinate = parameter.exp;
                    else if (parameter.name == nameof(CustomPropertiesVertex.vertex)
                             || parameter.name == nameof(CustomPropertiesEdge.index)
                             || parameter.name == nameof(CustomPropertiesPlacementSurfaceLowDistorsionMapping.triangle)
                             || parameter.name == nameof(CustomPropertiesPlacementSurfaceBarycentricCoordinates.triangle))
                        index = parameter.exp;
                    else
                        yield return parameter;
                }
                bool skinnedMesh = source.valueType == UnityEngine.VFX.VFXValueType.SkinnedMeshRenderer;
                var mesh = !skinnedMesh ? source : new VFXExpressionMeshFromSkinnedMeshRenderer(source);
                var meshVertexCount = new VFXExpressionMeshVertexCount(mesh);
                var meshIndexCount = new VFXExpressionMeshIndexCount(mesh);

                var threeUint = VFXOperatorUtility.ThreeExpression[UnityEngine.VFX.VFXValueType.Uint32];

                if (spawnMode == SpawnMode.Custom)
                {
                    if (placementMode == SampleMesh.PlacementMode.Vertex)
                        index = VFXOperatorUtility.ApplyAddressingMode(index, meshVertexCount, mode);
                    else if (placementMode == SampleMesh.PlacementMode.Edge)
                        index = VFXOperatorUtility.ApplyAddressingMode(index, meshIndexCount, mode);
                    else if (placementMode == SampleMesh.PlacementMode.Surface)
                        index = VFXOperatorUtility.ApplyAddressingMode(index, meshIndexCount / threeUint, mode);
                }
                else if (spawnMode == SpawnMode.Random)
                {
                    if (placementMode == SampleMesh.PlacementMode.Vertex)
                    {
                        index = BuildRandomUIntPerParticle(meshVertexCount, 0);
                    }
                    else if (placementMode == SampleMesh.PlacementMode.Edge)
                    {
                        index = BuildRandomUIntPerParticle(meshIndexCount, 1);
                        coordinate = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false, new RandId(this, 2));
                    }
                    else if (placementMode == SampleMesh.PlacementMode.Surface)
                    {
                        index = BuildRandomUIntPerParticle(meshIndexCount / threeUint, 3);
                        var x = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false, new RandId(this, 4));
                        var y = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false, new RandId(this, 5));
                        coordinate = new VFXExpressionCombine(x, y);
                    }
                }

                var vertexAttributes = new[] { VertexAttribute.Position, VertexAttribute.Normal };
                VFXExpression[] sampling = null;
                if (placementMode == SampleMesh.PlacementMode.Vertex)
                    sampling = SampleMesh.SampleVertexAttribute(source, index, vertexAttributes).ToArray();
                else if (placementMode == SampleMesh.PlacementMode.Edge)
                    sampling = SampleMesh.SampleEdgeAttribute(source, index, coordinate, vertexAttributes).ToArray();
                else if (placementMode == SampleMesh.PlacementMode.Surface)
                    sampling = SampleMesh.SampleTriangleAttribute(source, index, coordinate, surfaceCoordinates, vertexAttributes).ToArray();

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

                if (spawnMode != SpawnMode.Custom || placementMode != SampleMesh.PlacementMode.Surface)
                    yield return nameof(surfaceCoordinates);

                if (spawnMode != SpawnMode.Custom)
                    yield return nameof(mode);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;

                if (sourceMesh == SampleMesh.SourceType.Mesh)
                    properties = properties.Concat(PropertiesFromType(nameof(CustomPropertiesMesh)));
                else
                    properties = properties.Concat(PropertiesFromType(nameof(CustomPropertiesPropertiesSkinnedMeshRenderer)));

                if (spawnMode == SpawnMode.Custom)
                {
                    if (placementMode == SampleMesh.PlacementMode.Vertex)
                        properties = properties.Concat(PropertiesFromType(nameof(CustomPropertiesVertex)));
                    else if (placementMode == SampleMesh.PlacementMode.Edge)
                        properties = properties.Concat(PropertiesFromType(nameof(CustomPropertiesEdge)));
                    else if (placementMode == SampleMesh.PlacementMode.Surface)
                    {
                        if (surfaceCoordinates == SampleMesh.SurfaceCoordinates.Barycentric)
                            properties = properties.Concat(PropertiesFromType(nameof(CustomPropertiesPlacementSurfaceBarycentricCoordinates)));
                        else if (surfaceCoordinates == SampleMesh.SurfaceCoordinates.Uniform)
                            properties = properties.Concat(PropertiesFromType(nameof(CustomPropertiesPlacementSurfaceLowDistorsionMapping)));
                        else
                            throw new InvalidOperationException("Unexpected surface coordinate mode : " + surfaceCoordinates);
                    }
                    else
                        throw new InvalidOperationException("Unexpected placement mode : " + placementMode);
                }

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
