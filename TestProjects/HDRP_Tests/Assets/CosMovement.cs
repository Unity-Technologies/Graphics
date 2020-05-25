using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CosMovement : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = new Vector3(Mathf.Cos(Time.realtimeSinceStartup)*5,1.5f + Mathf.Cos(3*Time.realtimeSinceStartup) * 1,0);
    }
}
