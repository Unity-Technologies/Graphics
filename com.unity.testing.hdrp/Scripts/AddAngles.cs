using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddAngles : MonoBehaviour
{

    [SerializeField] Vector3 anglesToAdd = new Vector3(0f, 0f, 0f);
    [SerializeField] int waitFrames = 0;
	
	Vector3 startAngles = Vector3.zero;
    int localFrameCount = 0;
	
	// Use this for initialization
    void Start()
    {
        startAngles = transform.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        
		if(localFrameCount == waitFrames){
			transform.eulerAngles = startAngles + anglesToAdd;
			Destroy(this);
		}
		
		localFrameCount++;
       
    }
}
