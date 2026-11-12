using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Fogbugz988062 : MonoBehaviour
{
    public Texture2D tex;

    void Start()
    {
        var prop = new MaterialPropertyBlock();
        prop.SetTexture("_MainTex", tex);
        prop.SetFloat("_AddRed", 1);
        GetComponent<TilemapRenderer>().SetPropertyBlock(prop);
    }
}
