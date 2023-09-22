using System;
using UnityEngine;
using UnityEngine.VFX;

public class Instancing512TextureChange : MonoBehaviour
{
    public int[] childIndices;
    public string textureName;
    public Texture texture;

    void Start()
    {
        foreach (int childIndex in childIndices)
        {
            Transform child = transform.GetChild(childIndex);
            if (child && child.TryGetComponent(out VisualEffect vfx))
            {
                vfx.SetTexture(textureName, texture);
            }
        }
    }
}
