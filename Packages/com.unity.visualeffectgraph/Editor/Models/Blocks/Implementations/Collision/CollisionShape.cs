using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

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
        private CollisionBase.Behavior behavior;

        public CollisionShapeSubVariants(CollisionBase.Behavior behavior)
        {
            this.behavior = behavior;
        }

        public override IEnumerable<Variant> GetVariants()
        {
            foreach (CollisionShapeBase.Type shape in Enum.GetValues(typeof(CollisionShapeBase.Type)))
            {
                if (shape == CollisionShapeBase.Type.SignedDistanceField) // defined as a main variant
                    continue;

                yield return new Variant(
                    CollisionBase.GetNamePrefix(behavior) + CollisionShapeBase.GetName(shape),
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
            foreach (var v in CollisionBase.preVariants)
            {
                yield return new Variant(
                    CollisionBase.GetNamePrefix(v.behavior) + "Shape",
                    v.category,
                    typeof(CollisionShape),
                    new[]
                    {
                            new KeyValuePair<string, object>("behavior", v.behavior),
                            new KeyValuePair<string, object>("shape", CollisionShapeBase.Type.Sphere),
                    },
                    () => new CollisionShapeSubVariants(v.behavior));

                yield return new Variant(
                    CollisionBase.GetNamePrefix(v.behavior) + "Signed Distance Field",
                    v.category,
                    typeof(CollisionShape),
                    new[]
                    {
                            new KeyValuePair<string, object>("behavior", v.behavior),
                            new KeyValuePair<string, object>("shape", CollisionShapeBase.Type.SignedDistanceField),
                    });
            }
        }
    }


    [VFXInfo(category = "Collision", variantProvider = typeof(CollisionShapeVariants))]
    sealed class CollisionShape : CollisionBase
    {
        public override string name => GetNamePrefix(behavior) + CollisionShapeBase.GetName(shape);

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
