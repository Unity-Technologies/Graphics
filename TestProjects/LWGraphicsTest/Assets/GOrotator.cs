using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GOrotator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 rot = transform.eulerAngles;
        //rot.y += 12.1f * Time.smoothDeltaTime;
        //rot.y += 12.1f * Time.deltaTime;
        rot.y += 0.5f;
        transform.eulerAngles = rot;
    }
}
