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
    interface ISpaceable
    {
        CoordinateSpace space { get; set; }
    }

    [VFXType]
    struct Sphere : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 center;
        public float radius;
    }

    [VFXType]
    struct OrientedBox : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 center;
        public Vector3 angles;
        public Vector3 size;
    }

    [VFXType]
    struct AABox : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 center;
        public Vector3 size;
    }

    [VFXType]
    struct Plane : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 position;
        public Vector3 normal;
    }

    [VFXType]
    struct Cylinder : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3  position;
        public Vector3  direction;
        public float    radius;
        public float    height;
    }

    [VFXType]
    struct Transform : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 position;
        public Vector3 angles;
        public Vector3 scale;
    }

    [VFXType]
    struct Position : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 position;
    }

    [VFXType]
    struct DirectionType : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        public Vector3 direction;
    }

    [VFXType]
    struct Vector : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

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
