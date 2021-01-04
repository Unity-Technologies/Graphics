using UnityEngine;

public struct SNorm16x4
{
    public uint lo;
    public uint hi;

    public SNorm16x4(float _x, float _y, float _z, float _w)
    {
        Vector4 vi = new Vector3();
        vi.x = Mathf.Clamp(_x, -1, 1);
        vi.y = Mathf.Clamp(_y, -1, 1);
        vi.z = Mathf.Clamp(_z, -1, 1);
        vi.w = Mathf.Clamp(_w, -1, 1);
        vi = vi * 0x7fff;
        lo = (uint)((ushort)vi.x) | ((uint)((ushort)vi.y) << 16);
        hi = (uint)((ushort)vi.z) | ((uint)((ushort)vi.w) << 16);
    }
}
