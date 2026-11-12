using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetSpriteColors : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        GameObject.Find("rendererColor").GetComponent<SpriteRenderer>().color = Color.blue;
        GameObject.Find("materialColor").GetComponent<Renderer>().material.color = Color.red;
        GameObject.Find("blendColor").GetComponent<SpriteRenderer>().color = Color.blue;
        GameObject.Find("blendColor").GetComponent<Renderer>().material.color = Color.red;
    }

    // Update is called once per frame
    void Update()
    {
    }
}
