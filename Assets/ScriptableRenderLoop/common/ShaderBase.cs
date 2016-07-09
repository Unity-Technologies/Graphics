#if !__HLSL

using UnityEngine;


public struct Vec2
{
    public static implicit operator Vec2(Vector2 v) { return new Vec2(v.x, v.y); }
    public Vec2(Vec2 v) { x = v.x; y = v.y; }
    public Vec2(float fX, float fY) { x = fX; y = fY; }

    public float x, y;
};

public struct Vec3
{
    public static implicit operator Vec3(Vector3 v) { return new Vec3(v.x, v.y, v.z); }
    public static implicit operator Vec3(Vector4 v) { return new Vec3(v.x, v.y, v.z); }
    public Vec3(Vec3 v) { x = v.x; y = v.y; z = v.z; }
    public Vec3(float fX, float fY, float fZ) { x = fX; y = fY; z = fZ; }

    public float x, y, z;
};

public struct Vec4
{
    public static implicit operator Vec4(Vector4 v) { return new Vec4(v.x, v.y, v.z, v.w); }
    public static implicit operator Vec4(Vector3 v) { return new Vec4(v.x, v.y, v.z, 1.0f); }
    public Vec4(Vec4 v) { x = v.x; y = v.y; z = v.z; w = v.w; }
    public Vec4(float fX, float fY, float fZ, float fW) { x = fX; y = fY; z = fZ; w = fW; }

    public float x, y, z, w;
};

public struct Mat44
{
	public Mat44( Matrix4x4 m ) { c0 = new Vec4(m.GetColumn(0)); c1 = new Vec4(m.GetColumn(1)); c2 = new Vec4(m.GetColumn(2)); c3 = new Vec4(m.GetColumn(3)); }

	public Vec4 c0, c1, c2, c3;
};

#endif