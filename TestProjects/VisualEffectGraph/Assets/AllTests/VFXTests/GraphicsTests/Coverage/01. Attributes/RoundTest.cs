using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("0.5: " + Mathf.Round(0.5f));
        Debug.Log("1.5: " + Mathf.Round(1.5f));
        Debug.Log("2.5: " + Mathf.Round(2.5f));
        Debug.Log("2.5: " + Mathf.Round(2.5f));
        Debug.Log("4.5: " + Mathf.Round(4.5f));
        Debug.Log("5.5: " + Mathf.Round(5.5f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
