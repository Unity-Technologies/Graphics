using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGen : MonoBehaviour
{
    private int xCount = 10;
    private int yCount = 2;
    private GameObject[] gos;

    // Start is called before the first frame update
    void Start()
    {

        gos = new GameObject[xCount * yCount];

        float strength   = 1;
        float smoothness = 1;
        for(var y = 0; y < yCount; y++)
        {
            for(var x = 0; x < xCount; x++)
            {
                int i = y * yCount + x;
                if(y == 0)
                {
                    strength = x / (float)(xCount - 1);
                    smoothness = 1;
                }
                else
                {
                    smoothness = x / (float)(xCount - 1);
                    strength = 1;
                }

                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.position = new Vector3(x, y, 0);

                var material = go.GetComponent<Renderer>().material;
                material.color = new Color(1f, 0.16f, 0f);
                material.SetFloat("_Metallic", 1.0f);
                material.SetFloat("_Smoothness", 0.35f);

                material.SetFloat("_ClearCoat", 1.0f);
                material.EnableKeyword("_CLEARCOAT");
                material.SetFloat("_ClearCoatStrength", strength);
                material.SetFloat("_ClearCoatSmoothness", smoothness);
                gos[i] = go;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
