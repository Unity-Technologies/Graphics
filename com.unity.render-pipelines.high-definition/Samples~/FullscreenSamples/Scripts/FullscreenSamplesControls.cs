using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullscreenSamplesControls : MonoBehaviour
{
    
    Camera Cam;
    public GameObject[] Anchors;
    int index;



    // Start is called before the first frame update
    void Start()
    {
       Cam = GetComponent(typeof(Camera)) as Camera;
       ResetCam();        
    }

    private void ResetCam ()
    {
        index = 0;
        transform.position = Anchors[0].transform.position;
        transform.rotation = Anchors[0].transform.rotation;
    }


    // Update is called once per frame
    void Update()
    {
        /*if(Input.GetKey("right"))
        {
            index = Anchors.length ? 0 : index ++;
            print (index);

        }*/
        
       
    }
}
