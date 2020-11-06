using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wiggle : MonoBehaviour
{
	
	public float RotationSpeed = 0;
	public float ScaleSpeed = 0;
	public Vector3 ScaleVector = new Vector3(0,0,0);
	public float PositionSpeed = 0;
	private Vector3 initialPosition = Vector3.zero;
	private Vector3 initialScale = Vector3.zero;
	public float positionAmplitude = 0.1f;
	public Vector3 rotateAround = Vector3.up;
	
    // Start is called before the first frame update
    void Start()
    {
        initialPosition = this.transform.localPosition;
        initialScale = this.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
		this.transform.RotateAround(this.transform.position, Vector3.up, RotationSpeed * Time.deltaTime);
		float scaleFactor = Mathf.Sin(ScaleSpeed * Time.realtimeSinceStartup);
		float scaleX = ScaleVector.x > 0 ? ScaleVector.x * scaleFactor : 1;
		float scaleY = ScaleVector.y > 0 ? ScaleVector.y * scaleFactor : 1;
		float scaleZ = ScaleVector.z > 0 ? ScaleVector.z * scaleFactor : 1;
		this.transform.localScale = initialScale + new Vector3(scaleX, scaleY, scaleZ);
		
		if(rotateAround.x == 1){
			this.transform.localPosition = new Vector3(initialPosition.x, initialPosition.y + positionAmplitude*Mathf.Sin(PositionSpeed * Time.realtimeSinceStartup), initialPosition.z + positionAmplitude*Mathf.Cos(PositionSpeed * Time.realtimeSinceStartup));
		}
		
		if(rotateAround.y == 1){
			this.transform.localPosition = new Vector3(initialPosition.x + positionAmplitude*Mathf.Sin(PositionSpeed * Time.realtimeSinceStartup), initialPosition.y , initialPosition.z + positionAmplitude*Mathf.Cos(PositionSpeed * Time.realtimeSinceStartup));
		}
		
		if(rotateAround.z == 1){
			this.transform.localPosition = new Vector3(initialPosition.x + positionAmplitude*Mathf.Sin(PositionSpeed * Time.realtimeSinceStartup), initialPosition.y + positionAmplitude*Mathf.Cos(PositionSpeed * Time.realtimeSinceStartup) , initialPosition.z);
		}
    }
}
