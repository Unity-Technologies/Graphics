using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
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
    { }
    class MinMaxAttribute : PropertyAttribute
    {
        public readonly float min;
        public readonly float max;

        // Attribute used to make a float or int variable in a script be restricted to a specific range.
        public MinMaxAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    [VFXType(VFXTypeAttribute.Usage.Default, "Circle"), Serializable]
    struct TCircle
    {
        [Tooltip("Sets the transform of the circle.")]
        public Transform transform;
        [Tooltip("Sets the radius of the circle.")]
        public float radius;

        public static TCircle defaultValue = new TCircle { transform = Transform.defaultValue, radius = 1.0f };
    }

    [VFXType(VFXTypeAttribute.Usage.Default, "Arc Circle"), Serializable]
    struct TArcCircle
    {
        [Tooltip("Sets the Circle shape input.")]
        public TCircle circle;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the circle is used. The value is in radians.")]
        public float arc;

        public static TArcCircle defaultValue = new TArcCircle { circle = TCircle.defaultValue, arc = 2.0f * Mathf.PI };
    }

    //This type is only used in DistanceToSphere
    [VFXType(VFXTypeAttribute.Usage.ExcludeFromProperty, "Simple Sphere"), Serializable]
    struct Sphere
    {
        [Tooltip("Sets the center of the sphere."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("Sets the radius of the sphere.")]
        public float radius;

        public static Sphere defaultValue = new Sphere { radius = 1.0f };
    }

    [VFXType(VFXTypeAttribute.Usage.Default, "Sphere"), Serializable]
    struct TSphere
    {
        [Tooltip("Sets the transform of the sphere.")]
        public Transform transform;
        [Tooltip("Sets the radius of the sphere.")]
        public float radius;

        public static implicit operator TSphere(Sphere v)
        {
            return new TSphere()
            {
                transform = new Transform()
                {
                    position = v.center,
                    scale = Vector3.one
                },
                radius = v.radius
            };
        }

        public static TSphere defaultValue = new TSphere { transform = Transform.defaultValue, radius = 1.0f };
    }

    [VFXType(VFXTypeAttribute.Usage.Default, "Arc Sphere"), Serializable]
    struct TArcSphere
    {
        public TSphere sphere;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the sphere is used. The value is in radians.")]
        public float arc;

        public static TArcSphere defaultValue = new TArcSphere { sphere = TSphere.defaultValue, arc = 2.0f * Mathf.PI };
    }


    [VFXType(VFXTypeAttribute.Usage.Default, "Cone"), Serializable]
    struct TCone
    {
        [Tooltip("Sets the transform of the cone.")]
        public Transform transform;
        [Min(0.0f), Tooltip("Sets the base radius of the cone.")]
        public float baseRadius;
        [Min(0.0f), Tooltip("Sets the top radius of the cone.")]
        public float topRadius;
        [Tooltip("Sets the height of the cone.")]
        public float height;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the cone is used. The value is in radians.")]

        public static TCone defaultValue = new TCone { transform = Transform.defaultValue, baseRadius = 1.0f, topRadius = 0.1f, height = 1.0f };
    }

    [VFXType(VFXTypeAttribute.Usage.Default, "Arc Cone"), Serializable]
    struct TArcCone
    {
        [Tooltip("Sets the cone.")]
        public TCone cone;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the cone is used. The value is in radians.")]
        public float arc;

        public static TArcCone defaultValue = new TArcCone { cone = TCone.defaultValue, arc = 2.0f * Mathf.PI };
    }

    [VFXType(VFXTypeAttribute.Usage.Default, "Torus"), Serializable]
    struct TTorus
    {
        [Tooltip("Sets the transform of the torus.")]
        public Transform transform;
        [Tooltip("Sets the radius of the torus ring.")]
        public float majorRadius;
        [Tooltip("Sets the thickness of the torus ring.")]
        public float minorRadius;

        public static TTorus defaultValue = new TTorus { transform = Transform.defaultValue, majorRadius = 1.0f, minorRadius = 0.1f };
    }

    [VFXType(VFXTypeAttribute.Usage.Default, "Arc Torus"), Serializable]
    struct TArcTorus
    {
        [Tooltip("Sets the cone.")]
        public TTorus torus;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the torus is used.")]
        public float arc;

        public static TArcTorus defaultValue = new TArcTorus { torus = TTorus.defaultValue, arc = 2.0f * Mathf.PI };
    }

    [VFXType(VFXTypeAttribute.Usage.Default, "Oriented Box"), VFXSpace(SpaceableType.Matrix), Serializable]
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

    [VFXType(VFXTypeAttribute.Usage.Default, "Axis Aligned Box"), Serializable]
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

        public static implicit operator Matrix4x4(Transform t)
        {
            return Matrix4x4.TRS(t.position, Quaternion.Euler(t.angles), t.scale);
        }

        public static implicit operator Transform(Matrix4x4 m)
        {
            return new Transform()
            {
                position = m.GetPosition(),
                angles = m.rotation.eulerAngles,
                scale = m.lossyScale
            };
        }
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

    [VFXType(VFXTypeAttribute.Usage.Default, "Direction"), Serializable]
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

    [VFXType(VFXTypeAttribute.Usage.ExcludeFromProperty), Serializable]
    struct CameraBuffer
    {
        private Texture texture;

        public CameraBuffer(Texture texture)
        {
            this.texture = texture;
        }

        public static implicit operator Texture(CameraBuffer cameraBuffer)
        {
            return cameraBuffer.texture;
        }

        public static implicit operator CameraBuffer(Texture texture)
        {
            return new CameraBuffer(texture);
        }

        public static implicit operator int(CameraBuffer cameraBuffer)
        {
            return cameraBuffer.texture?.GetInstanceID() ?? 0;
        }

        public static implicit operator CameraBuffer(int id)
        {
            return new CameraBuffer((Texture)EditorUtility.InstanceIDToObject(id));
        }
    }

    [VFXType(VFXTypeAttribute.Usage.Default, "Camera"), Serializable]
    struct CameraType
    {
        [Tooltip("The camera's Transform in the world.")]
        public Transform transform;
        [Tooltip("Uses Orthographic projection.")]
        public bool orthographic;
        [Angle, Range(0.0f, Mathf.PI), Tooltip("The field of view is the height of the camera’s view angle, measured in degrees along the local Y axis.")]
        public float fieldOfView;
        [Min(0.0f), Tooltip("The near plane is the closest plane relative to the camera where drawing occurs.")]
        public float nearPlane;
        [Min(0.0f), Tooltip("The far plane is the furthest plane relative to the camera where drawing occurs.")]
        public float farPlane;
        [Min(0.0f), Tooltip("The orthographic size is half the size of the vertical viewing volume.")]
        public float orthographicSize;
        [Min(0.0f), Tooltip("The aspect ratio is the proportional relationship between the camera’s width and height.")]
        public float aspectRatio;
        [Min(0.0f), Tooltip("The width and height of the final viewport of the camera in pixels, after upscaling if applicable.")]
        public Vector2 pixelDimensions;
        [Min(0.0f), Tooltip("The width and height of the camera buffers in pixels, before upscaling if applicable.")]
        public Vector2 scaledPixelDimensions;
        [Tooltip("The lens shift along the x and y directions.")]
        public Vector2 lensShift;
        [Tooltip("The depth buffer of the camera, containing the rendered depth information.")]
        public CameraBuffer depthBuffer;
        [Tooltip("The color buffer of the camera, containing the rendered color information.")]
        public CameraBuffer colorBuffer;

        public static CameraType defaultValue = new CameraType {
            transform = Transform.defaultValue,
            fieldOfView = 60.0f * Mathf.Deg2Rad,
            nearPlane = 0.3f, farPlane = 1000.0f,
            aspectRatio = 1.0f,
            lensShift = Vector2.zero,
            orthographicSize = 5.0f,
            pixelDimensions = new Vector2(1920, 1080),
            scaledPixelDimensions = new Vector2(1920, 1080)
        };
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
