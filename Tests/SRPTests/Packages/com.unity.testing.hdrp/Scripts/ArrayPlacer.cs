using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class ArrayPlacer : MonoBehaviour
{
    [Header("Use Context Menu to place the array")]

    public GameObject source;

    public Vector3Int counts = new Vector3Int(3, 3, 3);
    public Vector3 offsets = new Vector3(1.5f, 1.5f, 1.5f);
    public Vector3 angles = Vector3.zero;
    public Vector3 scale = Vector3.one;

    [ContextMenu("Place")]
    void Place()
    {
        if (source == null || counts.x < 1 || counts.y < 1 || counts.z < 1) return;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        var pos = Vector3.zero;

        for (int x = 0; x < counts.x; x++)
        {
            pos.x = offsets.x * x;

            for (int y = 0; y < counts.y; y++)
            {
                pos.y = offsets.y * y;
                for (int z = 0; z < counts.z; z++)
                {
                    pos.z = offsets.z * z;

                    var o = Instantiate(source);
                    o.transform.parent = transform;
                    o.transform.localPosition = pos;
                    o.transform.localEulerAngles = angles;
                    o.transform.localScale = scale;
                }
            }
        }
    }
}
