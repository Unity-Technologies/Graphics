using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.VFX
{
    public class VFXTypeAttribute : Attribute
    {}

    enum CoordinateSpace
    {
        Local,
        Global,
        Camera,
        SpaceCount
    }
    interface Spaceable
    {
        CoordinateSpace space { get; set; }
    }

    [VFXType]
    struct Sphere : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 center;
        public float radius;
    }

    [VFXType]
    struct OrientedBox : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 center;
        public Vector2 angles;
        public Vector2 size;
    }

    [VFXType]
    struct AABox : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 center;
        public Vector2 size;
    }

    [VFXType]
    struct Plane : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 position;
        public Vector3 normal;
    }

    [VFXType]
    struct Cylinder : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3  position;
        public Vector3  direction;
        public float    radius;
        public float    height;
    }

    [VFXType]
    struct Transform : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 position;
        public Vector3 angles;
        public Vector3 scale;
    }

    [VFXType]
    struct Position : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 position;
    }

    [VFXType]
    struct Vector : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 vector;
    }

    [VFXType]
    struct FlipBook
    {
        public int x;
        public int y;
    }
}
