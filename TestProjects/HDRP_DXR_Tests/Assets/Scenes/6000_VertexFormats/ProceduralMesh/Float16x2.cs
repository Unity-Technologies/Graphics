using UnityEngine;

public struct Float16x2
{
    public ushort x;
    public ushort y;

    public Float16x2(float _x, float _y)
    {
        x = Mathf.FloatToHalf(_x);
        y = Mathf.FloatToHalf(_y);
    }
}
