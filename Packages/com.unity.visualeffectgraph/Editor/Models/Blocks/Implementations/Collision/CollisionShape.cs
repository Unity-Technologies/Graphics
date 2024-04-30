using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class CollisionShapeBase
    {
        public enum Type
        {
            Sphere,
            OrientedBox,
            Cone,
            Plane,
            SignedDistanceField,
        }

        public static string GetName(Type type)
        {
            switch (type)
            {
                case Type.Sphere: return "Sphere";
                case Type.OrientedBox: return "Box";
                case Type.Cone: return "Cone / Cylinder";
                case Type.Plane: return "Plane";
                case Type.SignedDistanceField: return "Signed Distance Field";
            }
            throw new InvalidOperationException("Unexpected type: " + type);
        }

        public static Type GetType(System.Type type)
        {
            if (type == typeof(CollisionSphere))
            {
                return Type.Sphere;
            }

            if (type == typeof(CollisionOrientedBox))
            {
                return Type.OrientedBox;
            }

            if (type == typeof(CollisionCone))
            {
                return Type.Cone;
            }

            if (type == typeof(CollisionPlane))
            {
                return Type.Plane;
            }

            if (type == typeof(CollisionSDF))
            {
                return Type.SignedDistanceField;
            }

            throw new NotImplementedException(type.ToString());
        }

        public static System.Type GetType(Type type)
        {
            switch (type)
            {
                case Type.Sphere: return typeof(CollisionSphere);
                case Type.OrientedBox: return typeof(CollisionOrientedBox);
                case Type.Cone: return typeof(CollisionCone);
                case Type.Plane: return typeof(CollisionPlane);
                case Type.SignedDistanceField: return typeof(CollisionSDF);
            }

            throw new InvalidOperationException("Unexpected type: " + type);
        }

        public virtual IEnumerable<VFXNamedExpression> GetParameters(CollisionBase collisionBase,
            IEnumerable<VFXNamedExpression> collisionBaseParameters)
        {
            return collisionBaseParameters;
        }

        public virtual string GetSource(CollisionBase collisionBase)
        {
            return string.Empty;
        }
    }

    class CollisionShapeSubVariants : VariantProvider
    {
        private readonly CollisionBase.Behavior behavior;
        private readonly CollisionShapeBase.Type mainShape;

        public CollisionShapeSubVariants(CollisionBase.Behavior behavior, CollisionShapeBase.Type mainShape)
        {
            this.behavior = behavior;
            this.mainShape = mainShape;
        }

        public override IEnumerable<Variant> GetVariants()
        {
            foreach (CollisionShapeBase.Type shape in Enum.GetValues(typeof(CollisionShapeBase.Type)))
            {
                if (shape == this.mainShape) // defined as a main variant
                    continue;
                if (shape == CollisionShapeBase.Type.SignedDistanceField) // Also listed independently
                    continue;

                yield return new Variant(
                    $"{CollisionBase.GetNamePrefix(behavior)} Shape".AppendLabel(CollisionShapeBase.GetName(shape)),
                    string.Empty,
                    typeof(CollisionShape),
                    new[]
                    {
                            new KeyValuePair<string, object>("behavior", behavior),
                            new KeyValuePair<string, object>("shape", shape),
                    });
            }
        }
    }

    class CollisionShapeVariants : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            var mainVariants = new Dictionary<CollisionBase.Behavior, CollisionShapeBase.Type[]>()
            {
                { CollisionBase.Behavior.Collision, new [] {CollisionShapeBase.Type.Plane, CollisionShapeBase.Type.SignedDistanceField }},
                { CollisionBase.Behavior.Kill, new [] {CollisionShapeBase.Type.Plane, CollisionShapeBase.Type.SignedDistanceField }},
                { CollisionBase.Behavior.None, new [] {CollisionShapeBase.Type.OrientedBox, CollisionShapeBase.Type.SignedDistanceField }},
            };

            foreach (var variant in mainVariants)
            {
                var isFirst = true;
                var baseName = CollisionBase.GetNamePrefix(variant.Key);
                var literal = $"{baseName} Shape";
                var category = variant.Key == CollisionBase.Behavior.Collision ? "Collision" : "Collision/".AppendSeparator(baseName, 0);
                foreach (var shape in variant.Value)
                {
                    yield return new Variant(
                        literal.AppendLabel(CollisionShapeBase.GetName(shape)),
                        category,
                        typeof(CollisionShape),
                        new[]
                        {
                            new KeyValuePair<string, object>("behavior", variant.Key),
                            new KeyValuePair<string, object>("shape", shape),
                        },
                        isFirst ? () => new CollisionShapeSubVariants(variant.Key, shape) : null);
                    isFirst = false;
                }
            }
        }
    }


    [VFXInfo(category = "Collision", variantProvider = typeof(CollisionShapeVariants))]
    sealed class CollisionShape : CollisionBase
    {
        public override string name => $"{GetNamePrefix(behavior)} Shape".AppendLabel(CollisionShapeBase.GetName(shape));

        [SerializeField, VFXSetting]
        CollisionShapeBase.Type shape = CollisionShapeBase.Type.Plane;

        CollisionShapeBase m_Shape = new CollisionPlane();

        private CollisionShapeBase GetOrRefreshShape()
        {
            var newType = CollisionShapeBase.GetType(shape);
            if (m_Shape == null || newType != m_Shape.GetType())
            {
                m_Shape = Activator.CreateInstance(newType) as CollisionShapeBase;
            }
            return m_Shape;
        }

        public override IEnumerable<VFXNamedExpression> parameters => GetOrRefreshShape().GetParameters(this, base.parameters);

        protected override string collisionDetection => GetOrRefreshShape().GetSource(this);

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var shapeProperty in PropertiesFromType(GetOrRefreshShape().GetType().GetRecursiveNestedType("InputProperties")))
                    yield return shapeProperty;

                foreach (var property in base.inputProperties)
                    yield return property;
            }
        }
    }
}
