using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotator : MonoBehaviour
{

    public GameObject target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPos = target.transform.position;
        targetPos.z -= 5.0f;

        transform.LookAt(target.transform);
        transform.Translate(Vector3.right * 0.2f * Time.smoothDeltaTime);

        //transform.RotateAround (targetPos,new Vector3(0.0f,1.0f,0.0f), 0.2f * Time.deltaTime);
    }
}
