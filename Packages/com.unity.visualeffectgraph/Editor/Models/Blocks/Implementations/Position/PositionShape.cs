using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class PositionShapeBase
    {
        public enum Type
        {
            Sphere,
            OrientedBox,
            Cone,
            Torus,
            Circle,
            Line,
            SignedDistanceField,
        }

        public static string GetName(Type type)
        {
            switch (type)
            {
                case Type.Sphere: return "Sphere";
                case Type.OrientedBox: return "Box";
                case Type.Cone: return "Cone / Cylinder";
                case Type.Torus: return "Torus";
                case Type.Circle: return "Circle";
                case Type.Line: return "Line";
                case Type.SignedDistanceField: return "Signed Distance Field";
            }
            throw new InvalidOperationException("Unexpected type: " + type);
        }

        public static System.Type GetType(Type type)
        {
            switch (type)
            {
                case Type.Sphere: return typeof(PositionSphere);
                case Type.OrientedBox: return typeof(PositionBox);
                case Type.Cone: return typeof(PositionCone);
                case Type.Torus: return typeof(PositionTorus);
                case Type.Circle: return typeof(PositionCircle);
                case Type.Line: return typeof(PositionLine);
                case Type.SignedDistanceField: return typeof(PositionSDF);
            }
            throw new InvalidOperationException("Unexpected type: " + type);
        }

        public virtual IEnumerable<VFXNamedExpression> GetParameters(PositionShape positionBase, List<VFXNamedExpression> allSlots)
        {
            throw new NotImplementedException();
        }

        public static VFXExpression CalculateVolumeFactor(PositionBase.PositionMode positionMode, VFXExpression radius, VFXExpression thickness, float thicknessDimensions)
        {
            VFXExpression factor = VFXValue.Constant(0.0f);

            switch (positionMode)
            {
                case PositionBase.PositionMode.Surface:
                    factor = VFXValue.Constant(0.0f);
                    break;
                case PositionBase.PositionMode.Volume:
                    factor = VFXValue.Constant(1.0f);
                    break;
                case PositionBase.PositionMode.ThicknessAbsolute:
                case PositionBase.PositionMode.ThicknessRelative:
                {
                    if (positionMode == PositionBase.PositionMode.ThicknessAbsolute)
                    {
                        thickness = thickness / radius;
                    }
                    factor = VFXOperatorUtility.Saturate(thickness);
                    break;
                }
            }

            return new VFXExpressionPow(VFXValue.Constant(1.0f) - factor, VFXValue.Constant(thicknessDimensions));
        }

        public virtual string GetSource(PositionShape positionBase)
        {
            return string.Empty;
        }

        public virtual bool hasBase => false;
        public virtual bool supportCustomSpawn => true;
        public virtual bool supportVolume => true;
    }

    class PositionShapeSubVariants : VariantProvider
    {
        private readonly PositionShapeBase.Type mainVariantType;

        public PositionShapeSubVariants(PositionShapeBase.Type type)
        {
            mainVariantType = type;
        }

        public override IEnumerable<Variant> GetVariants()
        {
            foreach (PositionShapeBase.Type shape in Enum.GetValues(typeof(PositionShapeBase.Type)))
            {
                if (shape == mainVariantType)
                    continue;
                if (shape == PositionShapeBase.Type.SignedDistanceField) // Already listed as main variant independently
                    continue;

                yield return new Variant(
                    "Set".Label(false).AppendLiteral("Position Shape", false).AppendLabel(PositionShapeBase.GetName(shape)),
                    string.Empty,
                    typeof(PositionShape),
                    new[] { new KeyValuePair<string, object>("shape", shape) }
                );
            }
        }
    }

    class PositionShapeVariants : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            var shapes = (PositionShapeBase.Type[])Enum.GetValues(typeof(PositionShapeBase.Type));

            yield return new Variant(
                "Set".Label(false).AppendLiteral("Position Shape", false).AppendLabel("Sphere", false),
                "Position Shape",
                typeof(PositionShape),
                new[] { new KeyValuePair<string, object>("shape", PositionShapeBase.Type.Sphere) },
                () => new PositionShapeSubVariants(PositionShapeBase.Type.Sphere)
            );
            yield return new Variant(
                "Set".Label(false).AppendLiteral("Position Shape", false).AppendLabel("Signed Distance Field", false),
                "Position Shape",
                typeof(PositionShape),
                new[] { new KeyValuePair<string, object>("shape", PositionShapeBase.Type.SignedDistanceField) }
            );
        }
    }

    [VFXInfo(category = "Attribute/position/Composition/{0}", variantProvider = typeof(PositionShapeVariants))]
    sealed class PositionShape : PositionBase
    {
        public override string name => VFXBlockUtility.GetNameString(compositionPosition).Label(false).AppendLiteral("Position Shape", false).AppendLabel(PositionShapeBase.GetName(shape));

        [SerializeField, VFXSetting] PositionShapeBase.Type shape = PositionShapeBase.Type.Sphere;

        [VFXSetting,
         Tooltip("Controls whether particles are spawned on the base of the cone, or throughout the entire volume.")]
        public HeightMode heightMode = HeightMode.Volume;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip(
             "Orient particles conform to the geometry of the mesh they are sampled from.\nThe AxisX/AxisY/AxisZ attributes and/or the attribute direction can be written.")]
        public Orientation applyOrientation = Orientation.Direction;

        //This field could be save as SerializableReference if we need to store additional data
        PositionShapeBase m_Shape;

        private PositionShapeBase GetOrRefreshShape()
        {
            var newType = PositionShapeBase.GetType(shape);
            if (m_Shape == null || newType != m_Shape.GetType())
            {
                m_Shape = Activator.CreateInstance(newType) as PositionShapeBase;
            }

            return m_Shape;
        }

        protected override bool needDirectionWrite => applyOrientation.HasFlag(Orientation.Direction);
        protected override bool needAxesWrite => applyOrientation.HasFlag(Orientation.Axes);

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var allSlots = new List<VFXNamedExpression>(GetExpressionsFromSlots(this));
                foreach (var parameter in allSlots)
                {
                    if (parameter.name == nameof(CustomPropertiesBlendPosition.blendPosition)
                        || parameter.name == nameof(CustomPropertiesBlendDirection.blendDirection)
                        || parameter.name == nameof(CustomPropertiesBlendAxes.blendAxes))
                        yield return parameter;
                }

                foreach (var shapeParameter in GetOrRefreshShape().GetParameters(this, allSlots))
                    yield return shapeParameter;
            }
        }

    public override string source => GetOrRefreshShape().GetSource(this);

        protected override bool supportsVolumeSpawning => GetOrRefreshShape().supportVolume;

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var settings in base.filteredOutSettings)
                    yield return settings;

                var currentShape = GetOrRefreshShape();
                if (!currentShape.hasBase)
                    yield return nameof(heightMode);

                if (!currentShape.supportCustomSpawn)
                    yield return nameof(spawnMode);

                if (shape != PositionShapeBase.Type.SignedDistanceField)
                {
                    yield return nameof(killOutliers);
                    yield return nameof(projectionSteps);
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var currentShape = GetOrRefreshShape();
                foreach (var shapeProperty in PropertiesFromType(currentShape.GetType().GetRecursiveNestedType("InputProperties")))
                    yield return shapeProperty;

                if (spawnMode == SpawnMode.Custom && currentShape.supportCustomSpawn)
                    foreach (var customProperty in PropertiesFromType(currentShape.GetType().GetRecursiveNestedType("CustomProperties")))
                        yield return customProperty;

                foreach (var property in base.inputProperties)
                    yield return property;
            }
        }

        // SDF specific
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies whether we want to kill particles whose position is off the desired surface or volume")]
        public bool killOutliers = false;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies the number of steps used by the block to project the particle on the surface of the SDF. This can impact performance, but can yield less outliers. "), Min(1u)]
        public uint projectionSteps = 2u;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                foreach (var a in base.attributes)
                    yield return a;

                if (shape == PositionShapeBase.Type.SignedDistanceField && killOutliers)
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);
            }
        }
    }
}
