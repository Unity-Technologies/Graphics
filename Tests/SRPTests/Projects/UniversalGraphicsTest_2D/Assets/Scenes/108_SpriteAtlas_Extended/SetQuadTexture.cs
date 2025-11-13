using UnityEngine;

public class SetQuadTexture : MonoBehaviour
{
    public Sprite sprite;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<MeshRenderer>().material.mainTexture = sprite.texture;
    }
}
