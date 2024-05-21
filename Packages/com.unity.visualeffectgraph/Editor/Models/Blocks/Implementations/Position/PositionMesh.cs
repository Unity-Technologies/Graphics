using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX.Operator;

namespace UnityEditor.VFX.Block
{
    class PositionMeshProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                "Set".Label(false).AppendLiteral("Position Mesh", false).AppendLabel("Mesh", false),
                "Position Shape",
                typeof(PositionMesh),
                new[]
                {
                    new KeyValuePair<string, object>("sourceMesh", SampleMesh.SourceType.Mesh),
                    new KeyValuePair<string, object>("compositionPosition", AttributeCompositionMode.Overwrite)
                });

            yield return new Variant(
                "Set".Label(false).AppendLiteral("Position Mesh", false).AppendLabel("Skinned Mesh", false),
                "Position Shape",
                typeof(PositionMesh),
                new[]
                {
                    new KeyValuePair<string, object>("sourceMesh", SampleMesh.SourceType.SkinnedMeshRenderer),
                    new KeyValuePair<string, object>("compositionPosition", AttributeCompositionMode.Overwrite)
                });
        }
    }

    [VFXHelpURL("Block-SetPosition(Mesh)")]
    [VFXInfo(variantProvider = typeof(PositionMeshProvider))]
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

        [VFXSetting, SerializeField, Tooltip("Specifies the transform to apply to the root bone retrieved from the Skinned Mesh Renderer.")]
        private SampleMesh.SkinnedRootTransform skinnedTransform = SampleMesh.SkinnedRootTransform.ApplyLocalRootTransform;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Orient particles conform to the geometry of the mesh they are sampled from.\nThe AxisX/AxisY/AxisZ attributes and/or the attribute direction can be written.")]
        private Orientation applyOrientation = Orientation.Direction;

        public override string name
        {
            get
            {
                if (sourceMesh == SampleMesh.SourceType.Mesh)
                    return VFXBlockUtility.GetNameString(compositionPosition).Label(false).AppendLiteral("Position Mesh", false).AppendLabel("Mesh");
                else
                    return VFXBlockUtility.GetNameString(compositionPosition).Label(false).AppendLiteral("Position Mesh", false).AppendLabel("Skinned Mesh");
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

        public class TransformProperties
        {
            [Tooltip("Sets the transform of the sampled Mesh. If used with a Skinned Mesh, it is applied after the transform of the root bone.")]
            public Transform transform = Transform.defaultValue;
        }

        protected override bool needDirectionWrite { get { return applyOrientation.HasFlag(Orientation.Direction); } }
        protected override bool needAxesWrite { get { return applyOrientation.HasFlag(Orientation.Axes); } }
        protected override bool supportsVolumeSpawning { get { return false; } }

        public override void Sanitize(int version)
        {
            base.Sanitize(version);

            if (version < 10)
            {
                Debug.Log("Sanitize Graph: Position Mesh & Skinned Mesh");
                //Insure SkinnedTransform is set to none and the transform will lead to identity
                SetSettingValue(nameof(skinnedTransform), SampleMesh.SkinnedRootTransform.None);
                var transformSlot = inputSlots.Last();
                transformSlot.space = GetOwnerSpace();
            }
        }

        private VFXExpression BuildRandomUIntPerParticle(VFXExpression max, int id)
        {
            //TODO : Add support of proper integer random
            var rand = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false, new RandId(this, id));
            VFXExpression r = new VFXExpressionCastFloatToUint(rand * new VFXExpressionCastUintToFloat(max));
            r = VFXOperatorUtility.ApplyAddressingMode(r, max, VFXOperatorUtility.SequentialAddressingMode.Clamp);
            return r;
        }

        private SampleMesh.SkinnedRootTransform actualSkinnedTransform
        {
            get
            {
                if (sourceMesh == SampleMesh.SourceType.SkinnedMeshRenderer)
                    return skinnedTransform;
                return SampleMesh.SkinnedRootTransform.None;
            }
        }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            var transformSlot = inputSlots.Last();
            if (actualSkinnedTransform == SampleMesh.SkinnedRootTransform.ApplyWorldRootTransform &&
                transformSlot.space == VFXSpace.Local)
            {
                report.RegisterError("MixingSMRWorldAndLocalPostTransformBlock", VFXErrorType.Warning, SampleMesh.kMixingSMRWorldAndLocalPostTransformMsg, this);
            }
        }

        private VFXSpace GetWantedOutputSpace()
        {
            //If we are applying the skinned mesh in world, using a local transform afterwards looks unexpected, forcing conversion of inputs to world.
            if (actualSkinnedTransform == SampleMesh.SkinnedRootTransform.ApplyWorldRootTransform)
                return VFXSpace.World;

            //Otherwise, the input slot transform control the owner space.
            return GetOwnerSpace();
        }

        //Warning: if GetOwnerSpace() != GetOutputSpaceFromSlot(), then, conversion *must* be handled in parameters computation
        public override VFXSpace GetOutputSpaceFromSlot(VFXSlot outputSlot)
        {
            if (outputSlot.spaceable)
                return GetWantedOutputSpace();

            return (VFXSpace)int.MaxValue;
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression source = null;
                VFXExpression index = null;
                VFXExpression coordinate = null;
                VFXExpression postTransform = null;
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
                    else if (parameter.name == nameof(TransformProperties.transform))
                        postTransform = parameter.exp;
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

                var transform = SampleMesh.ComputeTransformMatrix(source, actualSkinnedTransform, postTransform);

                var ownerSpace = GetOwnerSpace();
                var outputSpaceExpression = GetWantedOutputSpace();
                if (ownerSpace != outputSpaceExpression
                    && outputSpaceExpression != (VFXSpace)int.MaxValue
                    && ownerSpace != (VFXSpace)int.MaxValue)
                {
                    transform.current = ConvertSpace(transform.current, SpaceableType.Matrix, outputSpaceExpression);
                    transform.previous = ConvertSpace(transform.previous, SpaceableType.Matrix, outputSpaceExpression);
                }

                SampleMesh.VertexAttributeFlag[] vertexAttributes;
                if (applyOrientation.HasFlag(Orientation.Axes))
                    vertexAttributes = new[] { SampleMesh.VertexAttributeFlag.Transform };
                else if (applyOrientation.HasFlag(Orientation.Direction))
                    vertexAttributes = new[] { SampleMesh.VertexAttributeFlag.Position, SampleMesh.VertexAttributeFlag.Normal };
                else
                    vertexAttributes = new[] { SampleMesh.VertexAttributeFlag.Position };

                VFXExpression[] sampling = null;
                if (placementMode == SampleMesh.PlacementMode.Vertex)
                    sampling = SampleMesh.SampleVertexAttribute(source, index, vertexAttributes, transform).ToArray();
                else if (placementMode == SampleMesh.PlacementMode.Edge)
                    sampling = SampleMesh.SampleEdgeAttribute(source, index, coordinate, vertexAttributes, transform).ToArray();
                else if (placementMode == SampleMesh.PlacementMode.Surface)
                    sampling = SampleMesh.SampleTriangleAttribute(source, index, coordinate, surfaceCoordinates, vertexAttributes, transform).ToArray();

                if (applyOrientation.HasFlag(Orientation.Axes))
                {
                    var sourceTransform = sampling[0];

                    var i = new VFXExpressionMatrixToAxis(sourceTransform, VFXValue.Constant(0));
                    var j = new VFXExpressionMatrixToAxis(sourceTransform, VFXValue.Constant(1));
                    var k = new VFXExpressionMatrixToAxis(sourceTransform, VFXValue.Constant(2));
                    var p = new VFXExpressionMatrixToAxis(sourceTransform, VFXValue.Constant(3));

                    yield return new VFXNamedExpression(i, "readAxisX");
                    yield return new VFXNamedExpression(j, "readAxisY");
                    yield return new VFXNamedExpression(k, "readAxisZ");
                    yield return new VFXNamedExpression(p, "readPosition");
                }
                else if (applyOrientation.HasFlag(Orientation.Direction))
                {
                    yield return new VFXNamedExpression(sampling[0], "readPosition");
                    yield return new VFXNamedExpression(sampling[1], "readAxisY");
                }
                else
                {
                    yield return new VFXNamedExpression(sampling[0], "readPosition");
                }
            }
        }

        public override IEnumerable<int> GetFilteredOutEnumerators(string name)
        {
            if (name == nameof(compositionAxes))
            {
                yield return (int)AttributeCompositionMode.Add;
                yield return (int)AttributeCompositionMode.Multiply;
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

                if (sourceMesh != SampleMesh.SourceType.SkinnedMeshRenderer)
                    yield return nameof(skinnedTransform);
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

                properties = properties.Concat(PropertiesFromType(nameof(TransformProperties)));

                return properties;
            }
        }

        public override string source
        {
            get
            {
                string source = "";
                source += "\n" + VFXBlockUtility.GetComposeString(compositionPosition, "position", "readPosition", "blendPosition");

                if (applyOrientation.HasFlag(Orientation.Axes))
                {
                    source += "\n" + VFXBlockUtility.GetComposeString(compositionAxes, "axisX", "readAxisX", "blendAxes");
                    source += "\n" + VFXBlockUtility.GetComposeString(compositionAxes, "axisY", "readAxisY", "blendAxes");
                    source += "\n" + VFXBlockUtility.GetComposeString(compositionAxes, "axisZ", "readAxisZ", "blendAxes");
                }

                if (applyOrientation.HasFlag(Orientation.Direction))
                {
                    source += "\n" + VFXBlockUtility.GetComposeString(compositionDirection, "direction", "readAxisY", "blendDirection");
                }

                return source;
            }
        }
    }
}
