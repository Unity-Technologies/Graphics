using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.VFX
{
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

    struct Sphere : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 center;
        public float radius;
    }

    struct OrientedBox : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 center;
        public Vector2 angles;
        public Vector2 size;
    }
    struct AABox : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 center;
        public Vector2 size;
    }

    struct Plane : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 position;
        public Vector3 normal;
    }

    struct Cylinder : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3  position;
        public Vector3  direction;
        public float    radius;
        public float    height;
    }

    struct Transform : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 position;
        public Vector3 angles;
        public Vector3 scale;
    }
    struct Position : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 position;
    }
    struct Vector : Spaceable
    {
        CoordinateSpace Spaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 vector;
    }
}
