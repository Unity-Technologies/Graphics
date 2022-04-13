using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class Deprecated
    {
        public static readonly Type[] s_Types = new Type[]
        {
            typeof(Circle),
            typeof(ArcCircle),
            //Simple Sphere is still used for Conform & Distance to Sphere
            typeof(ArcSphere),
            typeof(Cylinder),
            typeof(Cone),
            typeof(ArcCone),
            typeof(Torus),
            typeof(ArcTorus)
        };
    }

    [VFXType(VFXTypeAttribute.Usage.ExcludeFromProperty, "Circle (Deprecated)"), Serializable]
    struct Circle
    {
        [Tooltip("Sets the center of the circle."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("Sets the radius of the circle.")]
        public float radius;

        public static Circle defaultValue = new Circle { radius = 1.0f };
    }

    [VFXType(VFXTypeAttribute.Usage.ExcludeFromProperty, "Arc Circle (Deprecated)"), Serializable]
    struct ArcCircle
    {
        [Tooltip("Sets the Circle shape input.")]
        public Circle circle;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the circle is used. The value is in radians.")]
        public float arc;

        public static ArcCircle defaultValue = new ArcCircle { circle = Circle.defaultValue, arc = 2.0f * Mathf.PI };
    }

    [VFXType(VFXTypeAttribute.Usage.ExcludeFromProperty, "Arc Sphere (Deprecated)"), Serializable]
    struct ArcSphere
    {
        public Sphere sphere;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the sphere is used. The value is in radians.")]
        public float arc;

        public static ArcSphere defaultValue = new ArcSphere { sphere = Sphere.defaultValue, arc = 2.0f * Mathf.PI };
    }


    [VFXType(VFXTypeAttribute.Usage.ExcludeFromProperty, "Cylinder (Deprecated)"), Serializable]
    struct Cylinder
    {
        [Tooltip("Sets the center of the cylinder."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("Sets the radius of the cylinder.")]
        public float radius;
        [Tooltip("Sets the height of the cylinder.")]
        public float height;

        public static Cylinder defaultValue = new Cylinder { radius = 1.0f, height = 1.0f };
    }

    [VFXType(VFXTypeAttribute.Usage.ExcludeFromProperty, "Cone (Deprecated)"), Serializable]
    struct Cone
    {
        [Tooltip("Sets the center of the cone."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Min(0.0f), Tooltip("Sets the base radius of the cone.")]
        public float radius0;
        [Min(0.0f), Tooltip("Sets the top radius of the cone.")]
        public float radius1;
        [Tooltip("Sets the height of the cone.")]
        public float height;

        public static Cone defaultValue = new Cone { radius0 = 1.0f, radius1 = 0.1f, height = 1.0f };
    }

    [VFXType(VFXTypeAttribute.Usage.ExcludeFromProperty, "Arc Cone (Deprecated)"), Serializable]
    struct ArcCone
    {
        [Tooltip("Sets the center of the cone."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Min(0.0f), Tooltip("Sets the base radius of the cone.")]
        public float radius0;
        [Min(0.0f), Tooltip("Sets the top radius of the cone.")]
        public float radius1;
        [Tooltip("Sets the height of the cone.")]
        public float height;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the cone is used. The value is in radians.")]
        public float arc;

        public static ArcCone defaultValue = new ArcCone { radius0 = 1.0f, radius1 = 0.1f, height = 1.0f, arc = 2.0f * Mathf.PI };
    }

    [VFXType(VFXTypeAttribute.Usage.ExcludeFromProperty, "Torus (Deprecated)"), Serializable]
    struct Torus
    {
        [Tooltip("Sets the center of the torus."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("Sets the radius of the torus ring.")]
        public float majorRadius;
        [Tooltip("Sets the thickness of the torus ring.")]
        public float minorRadius;

        public static Torus defaultValue = new Torus { majorRadius = 1.0f, minorRadius = 0.1f };
    }

    [VFXType(VFXTypeAttribute.Usage.ExcludeFromProperty, "Arc Torus (Deprecated)"), Serializable]
    struct ArcTorus
    {
        [Tooltip("Sets the center of the torus."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("Sets the radius of the torus ring.")]
        public float majorRadius;
        [Tooltip("Sets the thickness of the torus ring.")]
        public float minorRadius;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the torus is used.")]
        public float arc;

        public static ArcTorus defaultValue = new ArcTorus { majorRadius = 1.0f, minorRadius = 0.1f, arc = 2.0f * Mathf.PI };
    }
}
