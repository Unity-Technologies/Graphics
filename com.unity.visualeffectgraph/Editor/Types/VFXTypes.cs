using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [AttributeUsage(AttributeTargets.Struct)]
    class VFXTypeAttribute : Attribute
    {}

    enum SpaceableType
    {
        None,
        Position,
        Direction,
        Matrix,
        Vector
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct)]
    class VFXSpaceAttribute : PropertyAttribute
    {
        public readonly SpaceableType type;
        public VFXSpaceAttribute(SpaceableType type)
        {
            this.type = type;
        }
    }

    class ShowAsColorAttribute : Attribute
    {}

    class CoordinateSpaceInfo
    {
        public static readonly int SpaceCount = Enum.GetValues(typeof(VFXCoordinateSpace)).Length;
    }

    [VFXType, Serializable]
    struct Circle
    {
        [Tooltip("Sets the center of the circle."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("Sets the radius of the circle.")]
        public float radius;

        public static Circle defaultValue = new Circle { radius = 1.0f };
    }

    [VFXType, Serializable]
    struct ArcCircle
    {
        [Tooltip("Sets the Circle shape input.")]
        public Circle circle;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the circle is used. The value is in radians.")]
        public float arc;

        public static ArcCircle defaultValue = new ArcCircle { circle = Circle.defaultValue, arc = 2.0f * Mathf.PI };
    }

    [VFXType, Serializable]
    struct Sphere
    {
        [Tooltip("Sets the center of the sphere."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("Sets the radius of the sphere.")]
        public float radius;

        public static Sphere defaultValue = new Sphere { radius = 1.0f };
    }

    [VFXType, Serializable]
    struct ArcSphere
    {
        public Sphere sphere;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the sphere is used. The value is in radians.")]
        public float arc;

        public static ArcSphere defaultValue = new ArcSphere { sphere = Sphere.defaultValue, arc = 2.0f * Mathf.PI };
    }

    [VFXType, VFXSpace(SpaceableType.Matrix), Serializable]
    struct OrientedBox
    {
        [Tooltip("Sets the center of the box."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Angle, Tooltip("Sets the orientation of the box.")]
        public Vector3 angles;
        [Tooltip("Sets the size of the box along each axis.")]
        public Vector3 size;

        public static OrientedBox defaultValue = new OrientedBox { size = Vector3.one };
    }

    [VFXType, Serializable]
    struct AABox
    {
        [Tooltip("Sets the center of the box."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("Sets the size of the box along each axis.")]
        public Vector3 size;

        public static AABox defaultValue = new AABox { size = Vector3.one };
    }

    [VFXType, Serializable]
    struct Plane
    {
        public Plane(Vector3 direction) { position = Vector3.zero; normal = direction; }

        [Tooltip("Sets the position of the plane."), VFXSpace(SpaceableType.Position)]
        public Vector3 position;
        [Normalize, Tooltip("Sets the direction of the plane."), VFXSpace(SpaceableType.Direction)]
        public Vector3 normal;

        public static Plane defaultValue = new Plane { normal = Vector3.up };
    }

    [VFXType, Serializable]
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

    [VFXType, Serializable]
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

    [VFXType, Serializable]
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

        public static ArcCone defaultValue = new ArcCone { radius0 = 1.0f, radius1 = 0.1f, height = 1.0f, arc = 2.0f * Mathf.PI};
    }

    [VFXType, Serializable]
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

    [VFXType, Serializable]
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

        public static ArcTorus defaultValue = new ArcTorus { majorRadius = 1.0f, minorRadius = 0.1f, arc = 2.0f * Mathf.PI};
    }

    [VFXType, Serializable]
    struct Line
    {
        [Tooltip("Sets the start position of the line."), VFXSpace(SpaceableType.Position)]
        public Vector3 start;
        [Tooltip("Sets the end position of the line."), VFXSpace(SpaceableType.Position)]
        public Vector3 end;

        public static Line defaultValue = new Line { start = Vector3.zero, end = Vector3.left };
    }

    [VFXType, VFXSpace(SpaceableType.Matrix), Serializable]
    struct Transform
    {
        [Tooltip("Sets the transform position."), VFXSpace(SpaceableType.Position)]
        public Vector3 position;
        [Angle, Tooltip("Sets the euler angles of the transform.")]
        public Vector3 angles;
        [Tooltip("Sets the scale of the transform along each axis.")]
        public Vector3 scale;

        public static Transform defaultValue = new Transform { scale = Vector3.one };
    }

    [VFXType, Serializable]
    struct Position
    {
        [Tooltip("The position."), VFXSpace(SpaceableType.Position)]
        public Vector3 position;

        public static implicit operator Position(Vector3 v)
        {
            return new Position() { position = v };
        }

        public static implicit operator Vector3(Position v)
        {
            return v.position;
        }

        public static Position defaultValue = new Position { position = Vector3.zero };
    }

    [VFXType, Serializable]
    struct DirectionType
    {
        [Tooltip("The normalized direction."), VFXSpace(SpaceableType.Direction)]
        public Vector3 direction;

        public static implicit operator DirectionType(Vector3 v)
        {
            return new DirectionType() { direction = v };
        }

        public static implicit operator Vector3(DirectionType v)
        {
            return v.direction;
        }

        public static DirectionType defaultValue = new DirectionType { direction = Vector3.up };
    }

    [VFXType, Serializable]
    struct Vector
    {
        [Tooltip("The vector."), VFXSpace(SpaceableType.Vector)]
        public Vector3 vector;

        public static implicit operator Vector(Vector3 v)
        {
            return new Vector() { vector = v };
        }

        public static implicit operator Vector3(Vector v)
        {
            return v.vector;
        }

        public static Vector defaultValue = new Vector { vector = Vector3.zero };
    }

    [VFXType, Serializable]
    struct FlipBook
    {
        public int x;
        public int y;

        public static FlipBook defaultValue = new FlipBook { x = 4, y = 4 };
    }

    [VFXType, Serializable]
    struct CameraType
    {
        [Tooltip("The camera's Transform in the world.")]
        public Transform transform;
        [Angle, Range(0.0f, Mathf.PI), Tooltip("The field of view is the height of the camera’s view angle, measured in degrees along the local Y axis.")]
        public float fieldOfView;
        [Min(0.0f), Tooltip("The near plane is the closest plane relative to the camera where drawing occurs.")]
        public float nearPlane;
        [Min(0.0f), Tooltip("The far plane is the furthest plane relative to the camera where drawing occurs.")]
        public float farPlane;
        [Min(0.0f), Tooltip("The aspect ratio is the proportional relationship between the camera’s width and height.")]
        public float aspectRatio;
        [Min(0.0f), Tooltip("The width and height of the camera in pixels.")]
        public Vector2 pixelDimensions;
        [Tooltip("The depth buffer of the camera, containing the rendered depth information.")]
        public Texture2DArray depthBuffer;
        [Tooltip("The color buffer of the camera, containing the rendered color information.")]
        public Texture2DArray colorBuffer;

        public static CameraType defaultValue = new CameraType { transform = Transform.defaultValue, fieldOfView = 60.0f * Mathf.Deg2Rad, nearPlane = 0.3f, farPlane = 1000.0f, aspectRatio = 1.0f, pixelDimensions = new Vector2(1920, 1080) };
    }

    [VFXType, Serializable]
    struct TerrainType
    {
        [Tooltip("Sets the bounds of the Terrain.")]
        public AABox Bounds;
        [Tooltip("Sets the height map of the Terrain.")]
        public Texture2D HeightMap;
        [Tooltip("Sets the height of the Terrain.")]
        public float Height;
    }
}
